module rec DataUtils

open MongoDB.Driver
open MongoDB.FSharp
open BotModels
open System
open System.Configuration

do
    Serializers.Register()

type User = { Id: string
              Name: string }

type BotRequest = { User: User
                    Request: string 
                    Response: string
                    RequestLang: string 
                    ResponseLang: string
                    CreateDate: DateTime }

type LogEntry = { Payload: TranslatePayload
                  Error: string 
                  CreateDate: DateTime }

let buildLogEntry payload msg =
    { Payload = payload
      Error = msg 
      CreateDate = DateTime.UtcNow }

let insertLogEntry entry =
    let client         = buildClient ()
    let db             = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<LogEntry> "Logs"     
    collection.InsertOne entry    
    true

let buildRequest uId uName req resp reqLang respLang =
    { User = { Id = uId; Name = uName }
      Request = req
      Response = resp
      RequestLang = reqLang 
      ResponseLang = respLang 
      CreateDate = DateTime.UtcNow }

let insertRequest req =
    let client         = buildClient ()
    let db             = client.GetDatabase "DictBot"    
    let collection = db.GetCollection<BotRequest> "BotRequests"
    collection.InsertOne req
    true   
    
let buildClient (): MongoClient =
    let conStr = ConfigurationManager.AppSettings.["ConString"]
    MongoClient(conStr)

