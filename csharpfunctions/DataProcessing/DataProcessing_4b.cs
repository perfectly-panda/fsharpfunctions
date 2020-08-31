using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace csharpfunctions.DataProcessing
{
    public static class DataProcessing_4b
    {
        //http://localhost:7071/admin/functions/DataProcessing_4b
        [FunctionName("DataProcessing_4")]
        public static async Task Run([TimerTrigger("0 0 1 */1 * *")] TimerInfo myTimer,
            [CosmosDB(
                databaseName: "GithubStats",
                collectionName: "DocsRepo",
                ConnectionStringSetting = "CosmosDBConnection")]  IAsyncCollector<IssueDataV2> outputData,
            ILogger log)
        {
            log.LogInformation($"C# DataProcessing_4b function executed at: {DateTime.Now}");

            var client = new GitHubClient(new ProductHeaderValue("fsharpfunctions"));
            var tokenAuth = new Credentials(Environment.GetEnvironmentVariable("GithubToken")); // NOTE: not real token
            client.Credentials = tokenAuth;

            log.LogInformation("Retrieving Github Issues");

            var request = new SearchIssuesRequest()
            {
                PerPage = 100,
                Page = 1,
                Type = IssueTypeQualifier.Issue,
                State = ItemState.Open
            };
            request.Repos.Add("MicrosoftDocs", "azure-docs");
            var openIssues = await client.Search.SearchIssues(request);
            log.LogInformation($"Retrieved {openIssues.Items.Count()} Issues"); ;

            log.LogInformation(client.GetLastApiInfo().RateLimit.Remaining.ToString());

            var byPriority = GetIssueCount(openIssues.Items, "Pri")
                .ToDictionary(g => g.Key.Remove(0, 3), g => g.Count());

            var defaultLabel = new Label(1, null,  "Unknown", null, null, null, true);
            var byService = openIssues.Items
                .GroupBy(i => i.Labels.Where(l => l.Name.Contains("/svc")).FirstOrDefault() ?? defaultLabel)
                .Select(g => new ItemCount()
                {
                    Name = g.Key.Name,
                    Count = g.Count()
                });

            var document = new IssueDataV2()
            {
                Id = Guid.NewGuid().ToString(),
                Source = "CSharp",
                EntryType = "BySubService",
                Timestamp = DateTime.UtcNow,
                TotalOpenIssues = openIssues.Items.Count(),
                MissingTags = openIssues.Items.Count(o => o.Labels.Count() == 0),
                CountByPriority = byPriority,
                CountByService = serviceCount.Values.ToList()
            };

            await outputData.AddAsync(document);
        }

        private static string CleanServiceName(this string str)
        {
            return str.Substring(0, str.LastIndexOf('/'));
        }

        private static IEnumerable<IGrouping<string, Label>> GetIssueCount(IReadOnlyList<Issue> issues, string labelCheck)
        {
            return issues.SelectMany(o => o.Labels)
                .Where(l => l.Name.Contains(labelCheck))
                .GroupBy(p => p.Name);
        }
    }
}
