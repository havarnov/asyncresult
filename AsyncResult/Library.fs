
module AsyncResult

open System.Threading.Tasks

type AsyncResultBuilder() =
    member __.Return(value) = async { return Ok value }

    member __.ReturnFrom(asyncResult: Async<Result<_, _>>) : Async<Result<_, _>> =
        async {
            let! result = asyncResult
            return result
        }

    member __.Bind(asyncResult: Async<Result<_, 'TError>>, binder: 'T -> Async<Result<'TOk, 'TError>>) =
        async {
            let! result = asyncResult
            match result with
            | Ok v -> return! binder v
            | Error err -> return Error err
        }

    member __.Bind(task: Task, binder: unit -> Async<Result<'TOk, 'TError>>) =
        async {
            do! task |> Async.AwaitTask
            return! binder ()
        }

    member __.Bind(task: Task<'T>, binder: 'T -> Async<Result<'TOk, 'TError>>) : Async<Result<'TOk, 'TError>> =
        async {
            let! res = task |> Async.AwaitTask
            return! binder res
        }

    member __.Delay(f: unit -> Async<Result<_, _>>) = f()

    member __.TryWith(m: Async<Result<_, _>>, h) =
        async {
            try
                return! m
            with
                | e -> return! (h e)
        }

    member __.TryFinally(m: Async<Result<_, _>>, compensation) =
        async {
            try
                return! m
            finally
                compensation()
        }

let asyncResult = new AsyncResultBuilder()
