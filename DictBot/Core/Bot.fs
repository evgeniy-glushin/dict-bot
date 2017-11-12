namespace Core

open LanguageServices
open System.Linq
open DataUtils
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
        |> Async.StartAsTask     

    let execCmd cmd uid = async {
        return     
            match cmd with
            | Start -> "Welcome!"
            | Help -> "Help"
            | Learn -> startLearningSession uid
    }

    let startLearningSession uid =
        let capacity = 4
        
        let words = 
            popWords capacity uid
            |> Seq.map (fun x -> { Word = x.Word; Trans = x.Trans; Attempts = 0; Succeeded = false })
        
        { UserId = uid; Step = 0; CreateDate = DateTime.UtcNow; IsActive = true; Words = words }
        |> insertSession |> ignore

        "Not enough words"

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
                            { UserId = user.Id; Word = correctedSpelling; Trans = [{ Text = trans; Score = 1. }]; Lang = reqLang; TransLang = respLang; Trained = 0; Succeeded = 0; CreateDate = DateTime.UtcNow; Sourse = "Bot"; Version = "Azure V2" }
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