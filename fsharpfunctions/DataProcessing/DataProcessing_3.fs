namespace FSharpFunctions

open System
open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
open Octokit
open Microsoft.FSharpLu.Json

module DataProcessing_3 =
    let createClient headerValue token = 
        let client = ProductHeaderValue headerValue |> GitHubClient
        client.Credentials <- Credentials token
        client 
    
    let tagFilter (issue: Issue, searchString: string) =
        (issue.Labels) 
            |> Seq.find(fun f -> f.Name.Contains(searchString))

    [<FunctionName("DataProcessing_3")>]
    let run([<TimerTrigger("0 0 0 */1 * *")>]myTimer: TimerInfo, [<CosmosDB(databaseName= "GithubStats", collectionName= "DocsRepo", ConnectionStringSetting = "CosmosDBConnection")>]  outputData: IAsyncCollector<string>,  log: ILogger) =
        async {
            sprintf "F# DataProcessing_3 function executed at: %A" DateTime.Now |> log.LogInformation 

            let token = Environment.GetEnvironmentVariable "GithubToken"
            let client = createClient "fsharpfunctions" token

            log.LogInformation("Retrieving Github Issues")
            let request = SearchIssuesRequest( PerPage = 100, Page = 1, Type = Nullable IssueTypeQualifier.Issue, State =  Nullable ItemState.Open)
            request.Repos.Add("MicrosoftDocs", "azure-docs");
            let! openIssues = client.Search.SearchIssues request |> Async.AwaitTask

            sprintf "Retrieved %A Issues" openIssues.Items.Count |> log.LogInformation
            client.GetLastApiInfo().RateLimit.Remaining.ToString() |> log.LogInformation

            let issues = (openIssues.Items) |> Seq.cache

            let noTags = issues |> Seq.filter (fun f-> f.Labels.Count = 0)
            let byPriority = issues |> Seq.countBy (fun f -> 
                let tag = tagFilter (f, "Pri") 
                tag.Name )

            let byService = 
                issues 
                |> Seq.collect (fun f -> f.Labels) 
                |> Seq.filter (fun f -> f.Name.Contains("/svc"))  
                |> Seq.countBy (fun f -> f.Name.Substring(0, f.Name.LastIndexOf('/')))
          

            let document = {
                IssueData.Id = Guid.NewGuid().ToString(); 
                Source = FSharp; 
                EntryType = ByService; 
                Timestamp = DateTime.UtcNow;
                TotalOpenIssues = openIssues.Items.Count;
                MissingTags = Seq.length noTags;
                CountByPriority = Some ( byPriority  |> Map.ofSeq );
                CountByService = Some (  byService  |> Map.ofSeq );
            }

            do! Compact.serialize document |> outputData.AddAsync |> Async.AwaitTask
        } |> Async.StartAsTask

