namespace FSharpFunctions

open System
open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
open Octokit
open Microsoft.FSharpLu.Json

module DataProcessing_4 =
    let createClient headerValue token = 
        let client = ProductHeaderValue headerValue |> GitHubClient
        client.Credentials <- Credentials token
        client 
    
    let tagFilter (issue: Issue, searchString: string) =
        (issue.Labels) 
            |> Seq.find(fun f -> f.Name.Contains(searchString))
    let getFirstTag(issue: Issue, searchString: string) =
        (issue.Labels) 
            |> Seq.tryPick(fun f -> 
                if f.Name.Contains(searchString) then
                    Some(f)
                else None)
    let cleanName (tag: Label option) =
        match tag with 
         | Some t -> Some (t.Name.Substring(0, t.Name.LastIndexOf('/')))
         | None -> None
     
    let subServiceSeqToOptionList (os: seq<SubService option>): list<SubService> option = 
        os |> Seq.choose id |> Seq.toList |> Some


    [<FunctionName("DataProcessing_4")>]
    let run([<TimerTrigger("0 0 0 */1 * *")>]myTimer: TimerInfo, [<CosmosDB(databaseName= "GithubStats", collectionName= "DocsRepo", ConnectionStringSetting = "CosmosDBConnection")>]  outputData: IAsyncCollector<string>,  log: ILogger) =
        async {
            sprintf "F# DataProcessing_4 function executed at: %A" DateTime.Now |> log.LogInformation 

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
                |> Seq.map (fun f ->
                    let service = getFirstTag (f, "/svc")
                    match service with
                        | Some x -> (x.Name, getFirstTag (f, "/subsvc") |> cleanName)
                        | None -> ("Unknown", getFirstTag (f, "/subsvc") |> cleanName)
                )
                |> Seq.groupBy (fst)
                |> Seq.map ( fun f -> {
                    Name = if (fst f).LastIndexOf('/') > 0 then (fst f).Substring(0, (fst f).LastIndexOf('/')) else fst f;
                    Count = snd f |> Seq.length;
                    SubServiceCounts = 
                        snd f
                        |> Seq.countBy ( fun g -> snd g)
                        |> Seq.map (fun h -> 
                            match fst h with
                                | Some x -> {Name = x; Count = snd h;} |> Some
                                | None -> None)
                        |> subServiceSeqToOptionList
                }) |> Seq.toList |> Some
          

            let document = {
                IssueDataV2.Id = Guid.NewGuid().ToString(); 
                Source = FSharp; 
                EntryType = ByService; 
                Timestamp = DateTime.UtcNow;
                TotalOpenIssues = openIssues.Items.Count;
                MissingTags = Seq.length noTags;
                CountByPriority = Some ( byPriority  |> Map.ofSeq );
                CountByService = byService;
            }

            do! Compact.serialize document |> outputData.AddAsync |> Async.AwaitTask
        } |> Async.StartAsTask

