module rec Domain

open System
open MongoDB.Bson

type BotPayload =  
  { UserId: string 
    UserName: string
    Text: string }

type User =  
  { Id: string
    Name: string
    Lang: string
    CreateDate: DateTime 
    ChangeDate: DateTime }

type BotRequest =  
  { User: User
    Request: string 
    Response: string
    RequestLang: string 
    ResponseLang: string
    CreateDate: DateTime }

type Dictionary = 
  { Id: ObjectId
    UserId: string
    Word: string 
    Trans: Word seq
    Lang: string
    TransLang: string
    Trained: int
    Succeeded: int
    CreateDate: DateTime 
    ChangeDate: DateTime
    Sourse: string
    Version: string }

type Word = 
  { Text: string
    Score: double }

type LearningWord = 
  { WordId: ObjectId
    Word: string
    Trans: Word seq
    Succeeded: bool
    Attempts: int }

type LearningSession = 
  { Id: ObjectId
    UserId: string
    Idx: int
    CreateDate: DateTime
    ChangeDate: DateTime
    IsActive: bool
    Words: LearningWord seq }

//type Session =
//    | Learning of LearningSession

type RequestType =
    | Command of Command
    | Text of string
    | SessionRunning of LearningSession

type Command =
    | Start of User
    | Help
    | Learn

type LogEntry =  
  { Payload: BotPayload
    Error: string 
    CreateDate: DateTime }
