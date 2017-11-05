module rec BotModels

let createPayload id name txt = 
    { UserId = id; UserName = name; Text = txt }

type TranslatePayload = { UserId: string; UserName: string; Text: string }
