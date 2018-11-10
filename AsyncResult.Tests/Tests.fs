module Tests

open System.Threading.Tasks
open System.Data.Common
open System.Data.SQLite

open Xunit

open AsyncResult

[<Fact>]
let ``return async result`` () =
    let res = asyncResult {
        return 1
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``return from async result`` () =
    let res = asyncResult {
        return! async { return Ok 1 }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``return from result directly`` () =
    let res = asyncResult {
        return! Ok 1
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``return from result directly with error`` () =
    let res = asyncResult {
        return! Error 1
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Error 1, res)

[<Fact>]
let ``bind async result`` () =
    let res = asyncResult {
        let! v = async { return Ok 1 }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``bind async result with error`` () =
    let res = asyncResult {
        let! v = async { return Error "foobar" }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Error "foobar", res)

[<Fact>]
let ``do async result`` () =
    let res = asyncResult {
        let mutable v = 1
        do! async {
            v <- 2
            return Ok ()
        }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 2, res)

[<Fact>]
let ``do async result with error`` () =
    let res = asyncResult {
        let mutable v = 1
        do! async {
            v <- 2
            return Error "foobar"
        }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Error "foobar", res)

[<Fact>]
let ``do async with task result`` () =
    let res = asyncResult {
        let! v = Task.FromResult(Ok 1) |> Async.AwaitTask
        let mutable v = v
        do! async {
            v <- v + 1
            return Ok ()
        }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 2, res)

[<Fact>]
let ``do async with task result with error`` () =
    let res = asyncResult {
        let! v = Task.FromResult(Error "foobar") |> Async.AwaitTask
        let mutable v = v
        do! async {
            v <- v + 1
            return Ok ()
        }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Error "foobar", res)

[<Fact>]
let ``do async with task result directly`` () =
    let res = asyncResult {
        let! v = Task.FromResult(1)
        let mutable v = v
        do! async {
            v <- v + 1
            return Ok ()
        }
        return! async { return Ok v }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 2, res)

[<Fact>]
let ``try with async with task result directly`` () =

    let res = asyncResult {
        try
            let mutable v = 1
            do! async {
                v <- v + 1
                return Ok ()
            }
            failwithf "%i" v
            return ()
        with
            | Failure e -> return! async { return Error e }
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Error "2", res)

[<Fact>]
let ``try finally async with task result directly`` () =

    let mutable finallyCheck = false

    let res = asyncResult {
        try
            try
                let mutable v = 1
                do! async {
                    v <- v + 1
                    return Ok ()
                }
                failwithf "%i" v
                return ()
            with
                | Failure e -> return! async { return Error e }
        finally
            finallyCheck <- true
            ()
    }

    let res = Async.RunSynchronously res

    Assert.True(finallyCheck)
    Assert.Equal(Error "2", res)

[<Fact>]
let ``asyncResult with Sqlite`` () =
    let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;New=True;" 
    let connection = new SQLiteConnection(connectionStringMemory)

    let res: Async<Result<int, _>> = asyncResult {
        do! connection.OpenAsync().ContinueWith(fun _ -> Ok ())
        return 1
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``asyncResult with Task x`` () =

    let res: Async<Result<int, _>> = asyncResult {
        let! i = Task.FromResult(1)
        let! f = Task.FromResult(1.0)
        do! Task.CompletedTask
        return 1
    }

    let res = Async.RunSynchronously res

    Assert.Equal(Ok 1, res)

[<Fact>]
let ``asyncResult with Sqlite including insert data`` () =
    let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;New=True;" 

    let res: Async<Result<float, _>> = asyncResult {
        try
            use connection = new SQLiteConnection(connectionStringMemory)
            do! connection.OpenAsync()

            let create = "
                CREATE TABLE table1 (
                    column1 float);"

            let cmd = new SQLiteCommand(create, connection)
            let! _ = cmd.ExecuteNonQueryAsync()

            let insert = "INSERT INTO table1 (column1) VALUES (12.1);"
            let insertCmd= new SQLiteCommand(insert, connection)
            let! rowAffected = insertCmd.ExecuteNonQueryAsync()

            let select = "SELECT * FROM table1;"
            let insertCmd= new SQLiteCommand(select, connection)
            let! (reader: DbDataReader) = insertCmd.ExecuteReaderAsync()
            let! _ = reader.ReadAsync()
            let value = System.Convert.ToDouble(reader.["column1"])

            return value
        with
            | :? System.Data.SQLite.SQLiteException as ex ->
                return! async { return Error ex }
    }

    let res = Async.RunSynchronously res

    let res = match res with | Ok v -> v | Error e -> failwithf "%A" e

    Assert.Equal(12.1, res, 2)
