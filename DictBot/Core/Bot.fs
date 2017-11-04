namespace Core

open LanguageServices
open System.Linq
open System.Text.RegularExpressions

module rec Bot =    
    let respond payload =
        async {
            let srcLang = detectLang payload.Text
            let trgLang = targetLang srcLang
            
            let! correctedSpelling = correctSpelling payload.Text srcLang      
            let! translation = translate correctedSpelling trgLang

            let isSpellingCorrected = correctedSpelling <> payload.Text
            return if isSpellingCorrected then correctedSpelling + " - " + translation
                   else translation
        } |> Async.StartAsTask
        
    let correctSpelling str lang =
        async {
            let! (spelling: SpellingResponse) = checkSpelling str lang
            let tokens = spelling.flaggedTokens.AsEnumerable() |> Seq.toList
            
            return replace tokens str
        }
   
    let rec private replace (tokens: FlaggedToken list) (str: string) =
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
            replaceToken head suggestions |> replace tail
                            
    let private detectLang str =
        // TODO: improve
        match Regex.IsMatch(str.Trim().Replace(" ", ""), "^[a-zA-Z0-9]*$") with
        | true -> "en"
        | _ -> "ru"     

    let private targetLang = function
        | "ru" -> "en"
        | _ -> "ru"
    
    let private checkSpelling str lang =
        LangProvider.CheckSpelling(str, lang) |> Async.AwaitTask
    
    let private translate str lang =
        LangProvider.Translate(str, lang) |> Async.AwaitTask

    let createPayload id name txt = 
        { UserId = id; UserName = name; Text = txt }

    type TranslatePayload = { UserId: string; UserName: string; Text: string }