namespace Core

open LanguageServices
open System.Linq

module rec Bot =    
    let respond txt =
        async {
            let lang = "ru"
            let! correctedSpelling = correctSpelling txt lang      
            let! translation = translate correctedSpelling lang

            let isSpellingCorrected = correctedSpelling = txt
            return if isSpellingCorrected then translation
                   else correctedSpelling + " - " + translation      
        } |> Async.StartAsTask
        
    let correctSpelling txt lang =
        async {
            let! (spelling: SpellingResponse) = checkSpelling txt lang
            let tokens = spelling.flaggedTokens

            return if tokens.Count > 0 then tokens.First().suggestions.First().suggestion
                   else txt
        }
       
    
    let private checkSpelling txt lang =
        LangProvider.CheckSpelling(txt, lang) |> Async.AwaitTask
    
    let private translate txt lang =
        LangProvider.Translate(txt, lang) |> Async.AwaitTask
