module rec Repository

open Data

// Represents C# friendly Result type
type ResultHolder<'T>(res: Result<'T, string>) =
    member x.HasValue = 
        match res with
        | Ok _ -> true
        | Error _ -> false

    member x.Value =
        match res with
        | Ok r -> r
        | Error err -> invalidOp err

let toResult result =
    result
    |> Async.RunSynchronously
    |> (fun r -> ResultHolder(r))

let insertNewWord word =
    asyncInsertNewWord word
    |> toResult

let tryFindSession uid =
    asyncTryFindSession uid
    |> toResult

let tryFindWord w uid =
    asyncTryFindWord w uid
    |> toResult

let findUser uid =
    asyncFindUser uid
    |> toResult