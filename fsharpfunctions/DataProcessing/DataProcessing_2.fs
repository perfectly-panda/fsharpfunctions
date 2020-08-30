namespace FSharpFunctions

open System
open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
open Octokit
open Microsoft.FSharpLu.Json

module DataProcessing_2 =
    let createClient headerValue token = 
        let client = ProductHeaderValue headerValue |> GitHubClient
        client.Credentials <- Credentials token
        client 

    [<FunctionName("DataProcessing_2")>]
    let run([<TimerTrigger("0 0 0 */1 * *")>]myTimer: TimerInfo, [<CosmosDB(databaseName= "GithubStats", collectionName= "DocsRepo", ConnectionStringSetting = "CosmosDBConnection")>]  outputData: IAsyncCollector<string>,  log: ILogger) =
        async {
            sprintf "F# DataProcessing_2 function executed at: %A" DateTime.Now |> log.LogInformation 

            let token = Environment.GetEnvironmentVariable "GithubToken"
            let client = createClient "fsharpfunctions" token

            log.LogInformation("Retrieving Github Issues")
            let options = ApiOptions(PageSize = Nullable<int>(100), PageCount = Nullable<int>(1))

            let! openIssues = client.Issue.GetAllForRepository(owner ="MicrosoftDocs", name = "azure-docs", options = options) |> Async.AwaitTask
            
            sprintf "Retrieved %A Issues" openIssues.Count |> log.LogInformation
            client.GetLastApiInfo().RateLimit.Remaining.ToString() |> log.LogInformation

            let document = {
                Id = Guid.NewGuid().ToString(); 
                Source = FSharp; 
                EntryType = CountOnly; 
                Timestamp = DateTime.UtcNow;
                TotalOpenIssues = openIssues.Count;
                MissingTags = None;
                CountByPriority = None;
                CountByService = None;
            }

            do! Compact.serialize document |> outputData.AddAsync |> Async.AwaitTask
        } |> Async.StartAsTask
