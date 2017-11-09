module rec DataUtils

open MongoDB.Driver
open MongoDB.FSharp
open BotModels
open System
open System.Configuration

do
    Serializers.Register()

let insertLogEntry entry =
    let client         = buildClient ()
    let db             = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<LogEntry> "Logs"     
    collection.InsertOne entry    
    true

let insertRequest req =
    let client         = buildClient ()
    let db             = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<BotRequest> "BotRequests"
    collection.InsertOne req
    true   
    
let private buildClient (): MongoClient =
    let conStr = ConfigurationManager.AppSettings.["ConString"]
    MongoClient() // TODO: add real string for prod

