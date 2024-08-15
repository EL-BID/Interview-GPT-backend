using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InterviewAiFunction
{
    public class InterviewResponsesPublicApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResponsesPublicApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public InterviewResponsesPublicApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("InterviewResponsesPublic")]
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
                await response.WriteStringAsync("Bad arguments");
            }

            return response;
        }
    }
}
