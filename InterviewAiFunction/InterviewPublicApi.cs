using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace InterviewAiFunction
{
    public class InterviewPublicApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewPublicApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public InterviewPublicApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("InterviewPublicApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/interview")] HttpRequestData req)
        {

            var response = req.CreateResponse(HttpStatusCode.OK);
            var interviewUuid = req.Query["Uuid"];
            if (interviewUuid != null)
            {
                Interview interview = _context.Interview.Include("Questions").FirstOrDefault(x => x.Uuid == interviewUuid);
                await response.WriteAsJsonAsync(interview);
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);
            }
            return response;
        }
    }
}
