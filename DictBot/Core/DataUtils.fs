module DataUtils

open Domain
open System
open MongoDB.Bson

let buildNewWord uid word trans lang transLang src version =
    { Id = ObjectId.GenerateNewId()
      UserId = uid
      Word = word
      Trans = trans
      Lang = lang
      TransLang = transLang 
      Trained = 0
      Succeeded = 0
      CreateDate = DateTime.UtcNow 
      ChangeDate = DateTime.UtcNow 
      Sourse = src
      Version = version }

let buildNewSession uid words =
    { Id = ObjectId.GenerateNewId()
      UserId = uid
      Idx = 0
      IsActive = true
      CreateDate = DateTime.UtcNow
      ChangeDate = DateTime.UtcNow
      Words = words } 

let buildUser id name defLang =
    { Id = id
      Name = if String.IsNullOrEmpty name then "" else name
      Lang = defLang
      CreateDate = DateTime.UtcNow
      ChangeDate = DateTime.UtcNow }

let isNotNull (x: 'a) =
    Object.ReferenceEquals(null, x) |> not
