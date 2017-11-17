module DataAsync

open MongoDB.Driver
open Domain
open System
open System.Configuration
open DataUtils
open System.Linq
    
let private db () =
    let conStr = ConfigurationManager.AppSettings.["ConString"]
    let client = MongoClient(conStr)
    client.GetDatabase "DictBot"

let checkIfFound msg x =
    if isNotNull x then Ok x
    else Error msg

let asyncFindUser uid = async {
    let collection = db().GetCollection<User> "Users"
    let filter = Builders<User>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Id), uid));
    let! users = collection.Find(filterDefinition).Limit(Nullable<int>(1)).ToListAsync() |> Async.AwaitTask
    return users.FirstOrDefault() 
            |> checkIfFound (sprintf "Couldn't find user with id %s" uid)
}

let asyncInsertUser usr = async {
    let collection = db().GetCollection<User> "Users"
    do! collection.InsertOneAsync usr |> Async.AwaitTask
    return Ok usr    
}

let asyncInsertSession session = async {
    let collection = db().GetCollection<LearningSession> "Sessions"
    do! collection.InsertOneAsync session |> Async.AwaitTask    
    return Ok session
}

let asyncPopWords count uid succeededThreshold = async {
    let collection = db().GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.UserId), uid),
                                      filter.Lt((fun x -> x.Succeeded), succeededThreshold));

    let! words = collection.Find(filterDefinition)
                    .SortBy(fun x -> x.Succeeded :> Object)
                    .ThenByDescending(fun x -> x.CreateDate :> Object)
                    .Limit(Nullable<int>(count))
                    .ToListAsync() |> Async.AwaitTask

    return
        match words.AsEnumerable() |> Seq.toList with
        | [] -> Error <| sprintf "No words found for %s." uid
        | list -> Ok list
}

let asyncTryFindSession uid = async {
    let collection = db().GetCollection<LearningSession> "Sessions"
    let filter = Builders<LearningSession>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.UserId), uid),
                                      filter.Eq((fun x -> x.IsActive), true));

    let! sessions = collection.Find(filterDefinition)
                      .SortByDescending(fun x -> x.CreateDate :> Object)
                      .Limit(Nullable<int>(1))
                      .ToListAsync() |> Async.AwaitTask
    
    return sessions.FirstOrDefault() 
            |> checkIfFound (sprintf "Couldn't find session for user with id %s." uid)
}

let asyncSaveSession session = async {
    let collection = db().GetCollection<LearningSession> "Sessions"
    let filter = Builders<LearningSession>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Id), session.Id));
    let! dRes = collection.DeleteOneAsync(filterDefinition) |> Async.AwaitTask
    if dRes.IsAcknowledged then
        do! collection.InsertOneAsync(session) |> Async.AwaitTask
        return Ok session
    else 
        return Error <| sprintf "Couldn't delete session %A." session.Id
}

let asyncUpdateWord (word: Dictionary) = async {
    let collection = db().GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter.Eq((fun x -> x.Id), word.Id)
    let update = Builders<Dictionary>.Update.Set((fun x -> x.Trained), word.Trained)
                                            .Set((fun x -> x.Succeeded), word.Succeeded)
                                            .Set((fun x -> x.ChangeDate), word.ChangeDate)

    let! uRes = collection.UpdateOneAsync(filter, update) |> Async.AwaitTask

    if (int)uRes.ModifiedCount = 1 then return Ok word
    else return Error <| sprintf "Couldn't update word %A." word.Id
}

let asyncTryFindWord word uid = async {
    let collection = db().GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word),
                                      filter.Eq((fun x -> x.UserId), uid));
    let! words = collection.Find(filterDefinition).Limit(Nullable<int>(1)).ToListAsync() |> Async.AwaitTask
    return words.FirstOrDefault()
           |> checkIfFound (sprintf "Couldn't find word %s for user %s" word uid) 
}

let asyncFindWordById id = async {
    let collection = db().GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Id), id));
    let! words = collection.Find(filterDefinition).Limit(Nullable<int>(1)).ToListAsync() |> Async.AwaitTask
    return words.FirstOrDefault()
           |> checkIfFound (sprintf "Couldn't find word %A" id)
}

let asyncInsertNewWord word = async {    
    let collection = db().GetCollection<Dictionary> "Dictionary"
    do! collection.InsertOneAsync word |> Async.AwaitTask
    return Ok word
}

let insertLogEntry entry =    
    let collection = db().GetCollection<LogEntry> "Logs"     
    collection.InsertOne entry    
    true

let insertRequest req =
    let db = db ()    
    let collection = db.GetCollection<BotRequest> "BotRequests"
    collection.InsertOne req
    true   

// for testing only
let tryFindWords word uid =
    let collection = db().GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word),
                                      filter.Eq((fun x -> x.UserId), uid));
    collection.Find(filterDefinition).ToEnumerable()

//let tryCatch f =
//    try
//        f() |> Ok
//    with
//        | Failure msg -> Error msg

//let (>>=) x f = Result.bind f x
