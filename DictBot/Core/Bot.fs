module Bot

open LanguageServices
open Language
open System.Linq
open DataUtils
open Data
open Domain
open System
open AsyncResult

let asyncDetectReqType (payload: BotPayload) = async {
    let startsWith (str: string) test =
        str.StartsWith(test)

    let mightBeCommand str =
       if startsWith str "/" then 
          let command = str.[1..]
          Ok command
       else
          Error str
   
    let! session = asyncTryFindSession payload.UserId
    let res = 
        match session with
        | Ok s -> SessionRunning s |> Ok
        | Error _ ->
            let msg = payload.Text.Trim().ToLower()
            match mightBeCommand msg with
            | Error _ -> Text payload.Text |> Ok 
            | Ok command -> 
                match command with
                | "start" -> 
                    let u = buildUser payload.UserId payload.UserName "en"
                    Command (Start u) |> Ok
                | "help" -> Command Help |> Ok
                | "learn" -> Command Learn |> Ok
                //| "setlang" -> 
                | _ -> Error "unknown command"

    return res
}

let rec replaceAll (tokens: FlaggedToken list) (str: string) =
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

//TODO: return Result
let asyncCorrectSpelling str lang = async {
    let! (spelling: SpellingResponse) = asyncCheckSpelling str lang
    let tokens = spelling.flaggedTokens.AsEnumerable() |> Seq.toList
            
    return replaceAll tokens str
}

let asyncTranslate (payload: BotPayload) = async {
    let reqLang = detectLang payload.Text
    let respLang = targetLang reqLang

    let! correctedSpelling = asyncCorrectSpelling payload.Text reqLang
    let! dictWord = asyncTryFindWord correctedSpelling payload.UserId
    let! translation = 
        match dictWord with 
        | Ok w -> w.Trans 
                    |> Seq.sortByDescending (fun y -> y.Score) 
                    |> Seq.map (fun y -> y.Text) 
                    |> Seq.head
                    |> Async.retn
        | Error _ -> async {
            let! trans = asyncTranslateWord correctedSpelling respLang
            // TODO: check if the word is good enough to save
            let newWord = buildNewWord payload.UserId correctedSpelling [{ Text = trans; Score = 1. }] reqLang respLang "Bot" "Azure V2" 
            let! _ = asyncInsertNewWord newWord

            return trans
        }
    
    let buildResponse () =
        let isSpellingCorrected = correctedSpelling <> payload.Text
        if isSpellingCorrected then correctedSpelling + " - " + translation
        else translation

    return buildResponse() |> Ok
}

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

let asyncUpdateWordStatistic learningWord (succeeded: bool) = asyncResult {
    let! word = asyncFindWordById learningWord.WordId 
    let updatedWord = { word with ChangeDate = DateTime.UtcNow
                                  Trained = word.Trained + 1
                                  Succeeded = word.Succeeded + Convert.ToInt32(succeeded) }
    
    return! asyncUpdateWord updatedWord
}

let asyncDealWithSession session (payload: BotPayload) = async {
    let answer = payload.Text.Trim().ToLower()     
    
    let checkIfOk word = asyncResult {
        let succeeded = word.Trans |> Seq.exists (fun x -> x.Text.ToLower() = answer)
        let! _ = asyncUpdateWordStatistic word succeeded
        let! newSession = newSessionState session succeeded DateTime.UtcNow |> asyncSaveSession
        if newSession.IsActive then 
            return newSession.Words 
            |> Seq.toList
            |> List.item newSession.Idx
            |> (fun nextWord ->
                    if succeeded then "Correct! Try the next one <br/> " + nextWord.Word
                    else "Incorrect! Try the next one <br/> " + nextWord.Word)                       
        else 
            return "Correct! You are done."
    }

    match session.Words |> Seq.tryItem session.Idx with 
    | None -> return Error "Index out of bounds."
    | Some w -> return! checkIfOk w                  
}

let asyncStartLearningSession uid = asyncResult {
    let wordsCount, learned = 4, 4
                
    let! dictWords = asyncPopWords wordsCount uid learned
    let learnWords = dictWords |> Seq.map (fun x -> { WordId = x.Id
                                                      Word = x.Word
                                                      Trans = x.Trans 
                                                      Attempts = 0 
                                                      Succeeded = false })
                                    
    match Seq.toList learnWords with
    | [] -> return "Not enough words"
    | _ -> 
        let! _ = buildNewSession uid learnWords |> asyncInsertSession 
        let first = Seq.head learnWords
        return "Translate following words in English. <br/> " + first.Word.ToLower()
}

let asyncExecCmd cmd uid = async {
    match cmd with
    | Start(u) -> 
        let! _ = asyncInsertUser u
        return Ok "Which language would you like to learn?<br/>Please write 'en' or 'ru'"
    | Help -> return Ok "Help"
    | Learn -> return! asyncStartLearningSession uid
}

let asyncRespond (payload: BotPayload) = asyncResult {
    let! requestType = asyncDetectReqType payload

    match requestType with 
    | Text _ -> return! asyncTranslate payload
    | Command cmd -> return! asyncExecCmd cmd payload.UserId
    | SessionRunning session -> return! asyncDealWithSession session payload
}

let respondAsync payload =
    async {
        let! res = asyncRespond payload
        match res with
        | Ok x -> return x
        | Error msg -> return msg
    } |> Async.StartAsTask