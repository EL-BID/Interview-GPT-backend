using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InterviewAiFunction
{
    public class FnEmailTest
    {
        private readonly ILogger<FnEmailTest> _logger;

        public FnEmailTest(ILogger<FnEmailTest> logger)
        {
            _logger = logger;
        }

        [Function("FnEmailTest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "email-test")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            await EmailUtils.ExecuteMailgun("jdaniel.zarate@gmail.com", "Jose Daniel", "Jose Daniel", "https://www.google.com");
            await EmailUtils.ExecuteMailgun("joseza@iadb.org", "Jose Daniel", "Jose Daniel", "https://www.google.com");
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
