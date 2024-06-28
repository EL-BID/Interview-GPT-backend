using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InterviewAiFunction
{
    public class InterviewsApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewsApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public InterviewsApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("Interviews")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "interviews")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            // ADMIN ONLY.
            var interviews = _context.Interview.ToList();
            await response.WriteAsJsonAsync(interviews);
            return response;
        }
    }
}
