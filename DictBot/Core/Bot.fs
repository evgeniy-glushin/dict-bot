namespace Core

open LanguageServices
open System.Linq
open DataUtils
open Data
open Domain
open RequestPerser

module rec Bot =    
    open System

    let respondAsync (payload: BotPayload) =
        let reqTypeRes = detectReqType payload
        
        match reqTypeRes with
        | Error err -> returnA err
        | Ok request -> 
            match request with
            | Text _ -> translate payload
            | Command cmd -> execCmd cmd payload.UserId
            | SessionRunning session -> dealWithSession session payload
        |> Async.StartAsTask     

    let newSessionState old succeeded timestamp =
        let isEnd words =
            words 
            |> List.map (fun w -> w.Succeeded)
            |> List.reduce (fun acc cur -> acc && cur)

        let nextIndex words curIdx =
            let lastIdx = List.length words - 1

            let rec findNewIdx idx =
                if idx > lastIdx then findNewIdx 0
                elif words.[idx].Succeeded then idx + 1 |> findNewIdx
                else idx
        
            curIdx + 1 |> findNewIdx

        let words = Seq.toList old.Words
        let newWords =
            List.map (fun w -> 
                if w = words.[old.Idx] then { w with Succeeded = succeeded; Attempts = w.Attempts + Convert.ToInt32(succeeded) }
                else w) words
        
        let build isActive idx =
            { old with Idx = idx; ChangeDate = timestamp; IsActive = isActive; Words = newWords }

        newWords
        |> isEnd
        |> (function 
            | true -> build false old.Idx
            | false -> 
                nextIndex newWords old.Idx
                |> build true)

    let dealWithSession session payload = async {
        let answer = payload.Text.Trim().ToLower()     
        
        let res = match session.Words |> Seq.tryItem session.Idx with 
                    | None -> "Index out of bounds."
                    | Some w -> 
                        let succeeded = w.Trans |> Seq.exists (fun x -> x.Text.ToLower() = answer)

                        updateWordStatistic w succeeded |> ignore

                        let newSession = newSessionState session succeeded DateTime.UtcNow |> saveSession
                        if newSession.IsActive then 
                            newSession.Words 
                            |> Seq.toList
                            |> List.item newSession.Idx
                            |> (fun nextWord ->
                                    if succeeded then "Correct! Try the next one <br/> " + nextWord.Word
                                    else "Incorrect! Try the next one <br/> " + nextWord.Word)                       
                        else 
                            // 
                            "Correct! You are done."
        return res
    }

    let updateWordStatistic learningWord succeeded =
        let word = findWordById learningWord.WordId 
        let updatedWord = { word with ChangeDate = DateTime.UtcNow
                                      Trained = word.Trained + 1
                                      Succeeded = word.Succeeded + Convert.ToInt32 succeeded }
        updateWord updatedWord

    let execCmd cmd uid = async {
        return     
            match cmd with
            | Start -> "Welcome!"
            | Help -> "Help"
            | Learn -> startLearningSession uid
    }

    let startLearningSession uid =
        let wordsCount, learned = 4, 4
                
        let words = 
            popWords wordsCount uid learned
            |> Seq.map (fun x -> { WordId = x.Id; Word = x.Word; Trans = x.Trans; Attempts = 0; Succeeded = false })
        
        match Seq.toList words with
        | [] -> "Not enough words"
        | _ -> 
            buildNewSession uid words |> insertSession |> ignore
            let first = Seq.head words
            "Translate following words in English. <br/> " + first.Word.ToLower()

    let returnA x = async {
        return x
    }

    let translate payload = async {
        try 
            let reqLang = detectLang payload.Text
            let respLang = targetLang reqLang
            
            let user = { Id = payload.UserId; Name = userName payload.UserName }
            let! correctedSpelling = correctSpelling payload.Text reqLang      
            let! translation =
                match (tryFindWordOpt correctedSpelling user.Id) with
                | Some x -> x.Trans |> Seq.sortByDescending (fun y -> y.Score) |> Seq.map (fun y -> y.Text) |> Seq.head |> returnA 
                | None -> async {
                            let! trans = translateExt correctedSpelling respLang
                            // TODO: check if the word is good enough to save
                            buildNewWord user.Id correctedSpelling [{ Text = trans; Score = 1. }] reqLang respLang "Bot" "Azure V2"
                            |> insertNewWord |> ignore
                            return trans 
                        }                      
            
            { User = user; Request = payload.Text; Response = translation; RequestLang = reqLang; ResponseLang = respLang; CreateDate = DateTime.UtcNow }
            |> insertRequest |> ignore

            let buildResponse () =
                let isSpellingCorrected = correctedSpelling <> payload.Text
                if isSpellingCorrected then correctedSpelling + " - " + translation
                else translation

            return buildResponse()
        with
            | Failure msg -> 
                { Payload = payload; Error = msg; CreateDate = DateTime.UtcNow } 
                |> insertLogEntry |> ignore
                return "Something went wrong! We are already fixing it."
    }

    let userName name =
        if String.IsNullOrEmpty name then ""
        else name
        
    let correctSpelling str lang =
        async {
            let! (spelling: SpellingResponse) = checkSpelling str lang
            let tokens = spelling.flaggedTokens.AsEnumerable() |> Seq.toList
            
            return replaceAll tokens str
        }
   
    let rec private replaceAll (tokens: FlaggedToken list) (str: string) =
        let replaceToken (token: FlaggedToken) (suggestions: Suggestion list) =
            let bestSuggestion () = 
                suggestions |> List.sortByDescending (fun x -> x.score) |> List.head

            match suggestions with 
            | [] -> str
            | _ -> str.Replace(token.token, bestSuggestion().suggestion)

        match tokens with
        | [] -> str
        | head :: tail -> 
            let suggestions = head.suggestions.AsEnumerable() |> Seq.toList
            replaceToken head suggestions |> replaceAll tail
                            
    let private detectLang (str: string) =
        // TODO: improve
        let alfabetEn = "qwertyuiopasdfghjklzxcvbnm"
        match str.Any(fun s -> alfabetEn.Contains(s)) with
        | true -> "en"
        | _ -> "ru"     

    let private targetLang = function
        | "ru" -> "en"
        | _ -> "ru"
    
    let private checkSpelling str lang =
        LangProvider.CheckSpelling(str, lang) |> Async.AwaitTask
    
    let private translateExt str lang =
        LangProvider.Translate(str, lang) |> Async.AwaitTask