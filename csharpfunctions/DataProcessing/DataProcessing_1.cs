using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Octokit;

namespace csharpfunctions.DataProcessing
{
    public static class DataProcessing_1
    {
        [FunctionName("DataProcessing_1")]
        public static async void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [Blob("sample-images-md/$return", FileAccess.Write, Connection = "StorageConnectionAppSetting")] string outputData,
            ILogger log)
        {
            log.LogInformation($"C# DataProcessing_1 function executed at: {DateTime.Now}");
            var client = new GitHubClient(new ProductHeaderValue("fsharpfunctions"));
            var openRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open
            };
            var openIssues = await client.Issue.GetAllForRepository("azure-docs", "MicrosoftDocs", openRequest);
        }
    }
}
