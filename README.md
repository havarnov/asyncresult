[![Travis Build Status](https://travis-ci.org/havarnov/asyncresult.svg?branch=master)](https://travis-ci.org/havarnov/asyncresult)


# AsyncResult

A computional expression for `Async<Result<_, _>>`.

```fsharp
let res: Async<Result<float, _>> = asyncResult {
    try
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
```
