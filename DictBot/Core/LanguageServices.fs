module Language

open LanguageServices
open System.Linq

//TODO: return Result
let asyncCheckSpelling str lang =
    LangProvider.CheckSpelling(str, lang) |> Async.AwaitTask
   
//TODO: return Result
let asyncTranslateWord str lang =
    LangProvider.Translate(str, lang) |> Async.AwaitTask


let detectLang (str: string) =
    // TODO: improve
    let alfabetEn = "qwertyuiopasdfghjklzxcvbnm"
    match str.Any(fun s -> alfabetEn.Contains(s)) with
    | true -> "en"
    | _ -> "ru"     

let targetLang = function
    | "ru" -> "en"
    | _ -> "ru"
