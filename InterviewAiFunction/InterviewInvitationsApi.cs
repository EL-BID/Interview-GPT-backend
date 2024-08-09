using DarkLoop.Azure.Functions.Authorization;
using InterviewAiFunction.Serializers;
using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewInvitationsApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewInvitationsApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public InterviewInvitationsApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResultPublicApi>();
        }

        [Function("InterviewResults")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invitations")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;
            string interviewIdParam = req.Query["InterviewId"];
            try
            {
                int interviewId = int.Parse(interviewIdParam);
                if (dbCommons.IsUserInterviewId(interviewId, email))
                {
                    var invitations = _context.InterviewInvitation.Where(i=>i.InterviewId == interviewId).ToList();
                    await response.WriteAsJsonAsync(invitations);
                }
            }
            catch (Exception ex)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
            }
            return response;
        }
    }
}
