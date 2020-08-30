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
    public static class DataProcessing_4
    {
        //http://localhost:7071/admin/functions/DataProcessing_4
        [FunctionName("DataProcessing_4")]
        public static async Task Run([TimerTrigger("0 0 1 */1 * *")]TimerInfo myTimer,
            [CosmosDB(
                databaseName: "GithubStats",
                collectionName: "DocsRepo",
                ConnectionStringSetting = "CosmosDBConnection")]  IAsyncCollector<IssueDataV2> outputData,
            ILogger log)
        {
            log.LogInformation($"C# DataProcessing_4 function executed at: {DateTime.Now}");

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

            var byPriority = openIssues.Items
                .SelectMany(o => o.Labels)
                .Where(l => l.Name.Contains("Pri"))
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var byService = openIssues.Items
                .SelectMany(o => o.Labels)
                .Where(l => l.Name.EndsWith("/svc"))
                .GroupBy(p => p.Name.CleanServiceName())
                .ToDictionary(g => g.Key, g => g.Count());

            var serviceCount = new Dictionary<string, ItemCount>();

            foreach(var item in openIssues.Items)
            {
                var serviceLabel = item.Labels.Where(l => l.Name.EndsWith("/svc")).FirstOrDefault();
                if (serviceLabel != null)
                {
                    var serviceName = serviceLabel.Name.CleanServiceName();
                    if (!serviceCount.TryAdd(serviceName, new ItemCount() { Name = serviceName, Count = 1 }))
                    {
                        serviceCount[serviceName].Count++;
                    }

                    var subserviceLabel = item.Labels.Where(l => l.Name.EndsWith("/subsvc")).FirstOrDefault();
                    if (subserviceLabel != null)
                    {
                        var subserviceName = subserviceLabel.Name.CleanServiceName();

                        if (serviceCount[serviceName].Children == null)
                        {
                            serviceCount[serviceName].Children = new List<ItemCount>();
                        }

                        if (serviceCount[serviceName].Children.Where(s => s.Name == subserviceName).Any())
                        {

                            var index = serviceCount[serviceName].Children.IndexOf(new ItemCount() { Name = subserviceName });
                            if(index != -1)
                            {
                                serviceCount[serviceName].Children[index].Count++;
                            }
                        }
                        else
                        {
                            serviceCount[serviceName].Children.Add(new ItemCount() { Name = subserviceName, Count = 1 });
                        }
                    }
                }
            }

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
    }
}
