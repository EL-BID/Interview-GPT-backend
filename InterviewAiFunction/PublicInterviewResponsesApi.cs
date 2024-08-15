using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InterviewAiFunction
{
    public class PublicInterviewResponsesApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public PublicInterviewResponsesApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public PublicInterviewResponsesApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("PublicInterviewResponsesApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/responses")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            try
            {
                string invitationCode = req.Query["InvitationCode"];
                int sessionId = int.Parse(req.Query["SessionId"]);
                InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == invitationCode);
                if (dbCommons.IsValidInvitationForSession(invitation, sessionId))
                {
                    var interviewResponses = _context.InterviewResponse.Where(r => r.SessionId == sessionId);
                    await response.WriteAsJsonAsync(interviewResponses);
                }
                else
                {
                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                }
            }
            catch(Exception ex)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
            }
            return response;
        }
    }
}
