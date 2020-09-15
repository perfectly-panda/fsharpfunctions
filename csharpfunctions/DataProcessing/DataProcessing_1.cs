using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace csharpfunctions.DataProcessing
{
    public static class DataProcessing_1
    {
        //http://localhost:7071/admin/functions/DataProcessing_1
        [FunctionName("DataProcessing_1")]
        public static async Task Run([TimerTrigger("0 0 0 */1 * *")] TimerInfo myTimer,
            [CosmosDB(
                databaseName: "GithubStats",
                collectionName: "DocsRepo",
                ConnectionStringSetting = "CosmosDBConnection")]  IAsyncCollector<IssueData> outputData,
            ILogger log)
        {
            log.LogInformation($"C# DataProcessing_1 function executed at: {DateTime.Now}");

            var client = new GitHubClient(new ProductHeaderValue("fsharpfunctions"));
            var tokenAuth = new Credentials(Environment.GetEnvironmentVariable("GithubToken"));
            client.Credentials = tokenAuth;

            log.LogInformation("Retrieving Github Issues");

            var request = new SearchIssuesRequest() {
                PerPage = 100,
                Page = 1,
                Type = IssueTypeQualifier.Issue,
                State = ItemState.Open
            };

            request.Repos.Add("MicrosoftDocs", "azure-docs");
            var openIssues = await client.Search.SearchIssues(request);
            log.LogInformation($"Retrieved {openIssues.Items.Count()} Issues");;

            log.LogInformation(client.GetLastApiInfo().RateLimit.Remaining.ToString());

            
            var document = new IssueData()
            {
                id = Guid.NewGuid().ToString(),
                Source = "CSharp",
                EntryType = "CountOnly",
                Timestamp = DateTime.UtcNow,
                TotalOpenIssues = openIssues.Items.Count(),
                MissingTags = openIssues.Items.Count(o => o.Labels.Count() == 0)
            };

            await outputData.AddAsync(document);
        }
    }
}
