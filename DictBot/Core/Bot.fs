namespace Core

open LanguageServices
open System.Linq
open System.Text.RegularExpressions

module rec Bot =    
    let respond str =
        async {
            let srcLang = detectLang str
            let trgLang = targetLang srcLang
            
            let! correctedSpelling = correctSpelling str srcLang      
            let! translation = translate correctedSpelling trgLang

            let isSpellingCorrected = correctedSpelling <> str
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
        match Regex.IsMatch(str, "^[a-zA-Z0-9]*$") with
        | true -> "en"
        | _ -> "ru"     

    let private targetLang = function
        | "ru" -> "en"
        | _ -> "ru"
    
    let private checkSpelling str lang =
        LangProvider.CheckSpelling(str, lang) |> Async.AwaitTask
    
    let private translate str lang =
        LangProvider.Translate(str, lang) |> Async.AwaitTask
