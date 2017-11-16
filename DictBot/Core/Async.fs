﻿module Async

let map f xAsync = async {
    // get the contents of xAsync 
    let! x = xAsync 
    // apply the function and lift the result
    return f x
    }

let retn x = async {
    // lift x to an Async
    return x
    }

let apply fAsync xAsync = async {
    // start the two asyncs in parallel
    let! fChild = Async.StartChild fAsync
    let! xChild = Async.StartChild xAsync

    // wait for the results
    let! f = fChild
    let! x = xChild 

    // apply the function to the results
    return f x 
    }

let bind f xAsync = async {
    // get the contents of xAsync 
    let! x = xAsync 
    // apply the function but don't lift the result
    // as f will return an Async
    return! f x
    }


// TODO LIST:
// 1. watch PS
// 2. Read articles about monads 
// 3. Look into docs of telegram bot - test buttons
// 4. Re-factor the existing code
// 5. Add needed features
// 6. Add user friendly error messages



