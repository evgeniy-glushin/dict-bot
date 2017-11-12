module rec Domain

open System
open MongoDB.Bson.Serialization.Attributes

type BotPayload =  
  { UserId: string 
    UserName: string
    Text: string }

type User =  
  { Id: string
    Name: string }

type BotRequest =  
  { User: User
    Request: string 
    Response: string
    RequestLang: string 
    ResponseLang: string
    CreateDate: DateTime }

[<BsonIgnoreExtraElements>]
type Dictionary = 
  { UserId: string
    Word: string 
    Trans: Word seq
    Lang: string
    TransLang: string
    Trained: int
    Succeeded: int
    CreateDate: DateTime 
    Sourse: string
    Version: string }

type Word = 
  { Text: string
    Score: double }

type LearningWord = 
  { Word: string
    Trans: Word seq
    Succeeded: bool
    Attempts: int }

[<BsonIgnoreExtraElements>]
type LearningSession = 
  { UserId: string
    Step: int
    CreateDate: DateTime
    IsActive: bool
    Words: LearningWord seq }

//type Session =
//    | Learning of LearningSession

type RequestType =
    | Command of Command
    | Text of string

type Command =
    | Start
    | Help
    | Learn
    
type LogEntry =  
  { Payload: BotPayload
    Error: string 
    CreateDate: DateTime }
