using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InterviewAiFunction
{
    public class InterviewResponsesApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResponsesApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public InterviewResponsesApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("InterviewResponses")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/responses")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);

            string invitationCode = req.Query["InvitationCode"];
            if (invitationCode != null)
            {
                InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i=>i.InvitationCode== invitationCode);
                if (invitation != null)
                {
                    var interviewResponses = _context.InterviewResponse.Where(r => r.InterviewInvitationId == invitation.Id);
                    await response.WriteAsJsonAsync(interviewResponses);
                }
                else
                {
                    response = req.CreateResponse(HttpStatusCode.NotFound);

                }                
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.WriteString("Bad arguments");
            }

            return response;
        }
    }
}
