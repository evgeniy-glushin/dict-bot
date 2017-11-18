module AsyncResult

type AsyncResult() =
    member __.Bind(x, f) = async {
        let! res = x
        match res with
        | Ok resVal -> return! f resVal
        | Error err -> return Error err      
    }

    member __.Return(x) = async {
        return Ok x
    }

    member __.ReturnFrom(x) = async {
        return! x
    }

let asyncResult = new AsyncResult()
