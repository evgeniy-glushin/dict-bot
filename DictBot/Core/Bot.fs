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
            | Command cmd -> execCmd cmd
        |> Async.StartAsTask     

    let execCmd cmd = async {
        return     
            match cmd with
            | Start -> "Welcome!"
            | Help -> "Help"
    }

    let returnA x = async {
        return x
    }

    let translate payload = async {
        try 
            let reqLang = detectLang payload.Text
            let respLang = targetLang reqLang
            
            let! correctedSpelling = correctSpelling payload.Text reqLang      
            let! translation = translateExt correctedSpelling respLang
                  
            let user = { Id = payload.UserId; Name = userName payload.UserName }
            { User = user; Request = payload.Text; Response = translation; RequestLang = reqLang; ResponseLang = respLang; CreateDate = DateTime.UtcNow }
            |> insertRequest |> ignore

            { UserId = user.Id; Word = correctedSpelling; Trans = [{ Text = translation; Score = 1. }]; Lang = reqLang; TransLang = respLang; Trained = 0; Succeeded = 0; CreateDate = DateTime.UtcNow; Sourse = "Bot"; Version = "Azure V2" }
            |> insertNewWord |> ignore

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