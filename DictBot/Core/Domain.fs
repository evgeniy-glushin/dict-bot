module rec Domain

open System

type BotPayload = { 
    UserId: string 
    UserName: string 
    Text: string }

type User = { 
    Id: string
    Name: string }

type BotRequest = { 
    User: User
    Request: string 
    Response: string
    RequestLang: string 
    ResponseLang: string
    CreateDate: DateTime }

type RequestType =
    | Command of Command
    | Text of string

type Command =
    | Start
    | Help
    
type LogEntry = { 
    Payload: BotPayload
    Error: string 
    CreateDate: DateTime }
