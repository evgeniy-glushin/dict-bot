namespace Core

open LanguageServices
open System.Linq

module rec Bot =    
    let respond txt =
        async {
            let srcLang, trgLang = "en", "ru"
            let! correctedSpelling = correctSpelling txt srcLang      
            let! translation = translate correctedSpelling trgLang

            let isSpellingCorrected = correctedSpelling <> txt
            return if isSpellingCorrected then correctedSpelling + " - " + translation
                   else translation
        } |> Async.StartAsTask
        
    let correctSpelling txt lang =
        async {
            let! (spelling: SpellingResponse) = checkSpelling txt lang
            let tokens = spelling.flaggedTokens.AsEnumerable() |> Seq.toList
            
            return replace tokens txt
        }
   
    let rec replace (tokens: FlaggedToken list) (txt: string) =
        let replaceToken (token: FlaggedToken) (suggestions: Suggestion list) =
            let bestSuggestion () = 
                suggestions |> List.sortByDescending (fun x -> x.score) |> List.head

            match suggestions with 
            | [] -> txt
            | _ -> txt.Replace(token.token, bestSuggestion().suggestion)

        match tokens with
        | [] -> txt
        | head :: tail -> 
            let suggestions = head.suggestions.AsEnumerable() |> Seq.toList
            replaceToken head suggestions |> replace tail
                            
       


    let private checkSpelling txt lang =
        LangProvider.CheckSpelling(txt, lang) |> Async.AwaitTask
    
    let private translate txt lang =
        LangProvider.Translate(txt, lang) |> Async.AwaitTask
