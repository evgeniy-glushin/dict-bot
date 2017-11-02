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
            let tokens = spelling.flaggedTokens.AsEnumerable() |> Seq.toList

            let buildGuess () = 
                tokens
                |> List.sortBy (fun x -> x.offset)
                |> List.map (fun x -> x.suggestions.AsEnumerable() 
                                        |> Seq.toList 
                                        |> Seq.sortByDescending (fun x -> x.score)
                                        |> Seq.head)
                |> List.map (fun x -> x.suggestion)
                |> List.reduce (fun acc elem -> acc + " " + elem)

            return if List.length tokens > 0 then buildGuess()
                   else txt            
        }
   
    let private checkSpelling txt lang =
        LangProvider.CheckSpelling(txt, lang) |> Async.AwaitTask
    
    let private translate txt lang =
        LangProvider.Translate(txt, lang) |> Async.AwaitTask
