namespace FSharpFunctions

open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

module API_1 =
    [<FunctionName("API_1")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)>]req: HttpRequest) (log: ILogger) =
        log.LogInformation("F# API 1 function processed a request.")
        OkObjectResult("Hello from F#!")