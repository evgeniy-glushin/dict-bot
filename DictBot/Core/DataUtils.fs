module rec DataUtils

open MongoDB.Driver
open Domain
open System
open System.Configuration

// NOTE: isn't working
//do
//    Serializers.Register()

// TODO: return Option type
let tryFindWord word =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word));
    collection.Find(filterDefinition).Limit(Nullable<int>(1)).FirstOrDefault()

let tryFindWordOpt word =
    let res = tryFindWord word
    if isNotNull res then Some res  
    else None 

// TODO: consider refactoring
let tryFindWords word =
    let client = buildClient ()
    let db = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<Dictionary> "Dictionary"
    let filter = Builders<Dictionary>.Filter
    let filterDefinition = filter.And(filter.Eq((fun x -> x.Word), word));
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


let isNotNull x =
    Object.ReferenceEquals(null, x) |> not
