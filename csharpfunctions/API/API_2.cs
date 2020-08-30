using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;

namespace csharpfunctions
{
    public static class API_2
    {
        [FunctionName("API_2")]
        public static async Task<IEnumerable<object>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "API_2")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# API 2 function processed a request.");

            using(var conn = new SqlConnection(Environment.GetEnvironmentVariable("SQLDBConnection")))
            {
                var sql = "SELECT * FROM dbo.Colors";
                return await conn.QueryAsync(sql);
            }
        }
    }
}