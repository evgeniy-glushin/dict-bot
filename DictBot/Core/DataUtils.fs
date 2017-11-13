module rec DataUtils

open MongoDB.Driver
open Domain
open System
open System.Configuration
open MongoDB.Bson

// TODO: make all asynchronous 

// NOTE: isn't working
//do
//    Serializers.Register()

let insertSession session =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<LearningSession> "Sessions"
    collection.InsertOne session    
    true

let popWords count uid succeeded =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.UserId), uid),
                                      filter.Lt((fun x -> x.Succeeded), succeeded));

    collection.Find(filterDefinition)
              .SortBy(fun x -> x.Succeeded :> Object)
              .ThenByDescending(fun x -> x.CreateDate :> Object)
              .Limit(Nullable<int>(count))
              .ToEnumerable()

let tryFindSession uid =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<LearningSession> "Sessions"
    let filter = Builders<LearningSession>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.UserId), uid),
                                      filter.Eq((fun x -> x.IsActive), true));

    collection.Find(filterDefinition)
              .SortByDescending(fun x -> x.CreateDate :> Object)
              .Limit(Nullable<int>(1))
              .FirstOrDefault()

let tryFindSessionOpt uid = 
    let res = tryFindSession uid
    if isNotNull res then Some res
    else None

// TODO: return Option type
let tryFindWord word uid =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word),
                                      filter.Eq((fun x -> x.UserId), uid));
    collection.Find(filterDefinition).Limit(Nullable<int>(1)).FirstOrDefault()

let tryFindWordOpt word uid =
    let res = tryFindWord word uid
    if Object.ReferenceEquals(null, res)  then None 
    else Some res 

// TODO: consider refactoring
let tryFindWords word uid =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word),
                                      filter.Eq((fun x -> x.UserId), uid));
    collection.Find(filterDefinition).ToEnumerable()

let insertNewWord word =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    collection.InsertOne word    
    true

let insertLogEntry entry =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<LogEntry> "Logs"     
    collection.InsertOne entry    
    true

let insertRequest req =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<BotRequest> "BotRequests"
    collection.InsertOne req
    true   
    
let private buildClient (): MongoClient =
    let conStr = ConfigurationManager.AppSettings.["ConString"]
    MongoClient() // TODO: add real string for prod

let dropDatabase () =
    let client = MongoClient()
    let db = client.GetDatabase "DictBot"    
    //db.DropCollection "BotRequests"
    db.DropCollection "Dictionary"
    db.DropCollection "Sessions"

// TODO: make this really generic
let isNotNull (x: 'a) =
    Object.ReferenceEquals(null, x) |> not
