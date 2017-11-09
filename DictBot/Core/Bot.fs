namespace Core

open LanguageServices
open System.Linq
open DataUtils
open BotModels

module rec Bot =    
    open System

    let respond payload =
        async {
            try 
                let reqLang = detectLang payload.Text
                let respLang = targetLang reqLang
            
                let! correctedSpelling = correctSpelling payload.Text reqLang      
                let! translation = translate correctedSpelling respLang
                  
                let user = { Id = payload.UserId; Name = payload.UserName }
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
        } |> Async.StartAsTask
        
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
    
    let private translate str lang =
        LangProvider.Translate(str, lang) |> Async.AwaitTask