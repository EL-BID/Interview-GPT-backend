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
    public class PublicInterviewApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public PublicInterviewApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public PublicInterviewApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        [Function("PublicInterviewApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/interview")] HttpRequestData req)
        {

            var response = req.CreateResponse(HttpStatusCode.OK);
            var interviewUuid = req.Query["Uuid"];
            var invitationCode = req.Query["InvitationCode"];

            InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(x=>x.InvitationCode== invitationCode);
            Interview interview = _context.Interview.Include("Questions").FirstOrDefault(x => x.Id == invitation.InterviewId);
            if (interview != null)
            {
                InterviewSession currentSession = _context.InterviewSession.FirstOrDefault(x => x.SessionUser == invitation.Email && x.Status == "active" && x.InterviewId==invitation.InterviewId);
                if(currentSession == null)
                {
                    currentSession = new InterviewSession
                    {
                        InterviewId = invitation.InterviewId,
                        SessionUser = invitation.Email,
                        Status = "active",
                        CreatedAt = DateTime.Now,
                    };
                    _context.InterviewSession.Add(currentSession);
                    await _context.SaveChangesAsync();

                }
                await response.WriteAsJsonAsync(new {
                    interview = new
                    {
                        interview.Id,
                        interview.Description,                        
                        interview.Title,
                        interview.Prompt,
                        interview.Status,
                        interview.Questions,
                        interview.WelcomeTitle,
                        interview.WelcomeMessage,
                        interview.CompletedTitle,
                        interview.CompletedMessage
                    },
                    session = currentSession
                });
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);
            }
            return response;
        }
    }
}
