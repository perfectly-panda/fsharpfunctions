namespace fsharpfunctions

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open System.Data.SqlClient
open Dapper
open System.Data

module API_2 =

    let connectionString = @"" + Environment.GetEnvironmentVariable("dbConnection")

    type Color = {
        Name   : string
        Category : string
        Notes : string option
        Brands : string option
        Id : int
    } with 
        // actually these functions could exist on their own outside the record, 
        // I prefer to keep them together
        // create a new record from the current rdr position
        static member FromRdr(rdr:IDataReader) = {
                        Name     = rdr.GetString  0; 
                        Category    = rdr.GetString 1; 
                        Notes    = if rdr.IsDBNull 2 then None else Some(rdr.GetString 2);
                        Brands = if rdr.IsDBNull 3 then None else Some(rdr.GetString 3);
                        Id = rdr.GetInt32 4;
                      }                

    [<FunctionName("API_2")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)>]req: HttpRequest) (log: ILogger) =
        log.LogInformation("F# API 2 function processed a request.")
        seq {
            use conn = new SqlConnection(connectionString)
            conn.Open()
            let rdr = conn.ExecuteReader("SELECT * FROM dbo.Colors")
            while rdr.Read()
                do yield Color.FromRdr rdr
        }