module rec BotModels

open System

type TranslatePayload = { 
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

type LogEntry = { 
    Payload: TranslatePayload
    Error: string 
    CreateDate: DateTime }