using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace csharpfunctions
{
    public static class API_2
    {
        [FunctionName("API_2")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "API_2/{message}")] HttpRequest req, string message,
            ILogger log)
        {
            log.LogInformation("C# API 1 function processed a request.");
            return new OkObjectResult(message);
        }
    }
}