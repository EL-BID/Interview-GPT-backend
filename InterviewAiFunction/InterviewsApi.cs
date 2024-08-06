using DarkLoop.Azure.Functions.Authorization;
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
    [FunctionAuthorize]
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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "interviews")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var includeAll = req.Query["All"];

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (req.Identities.Any())
            {
                var email = req.Identities.First().Name;
                if (includeAll != null && includeAll == "yes")
                {
                    var interviews = _context.Interview.Where(i => i.CreatedBy.ToLower() == email.ToLower()).Select(
                        i => new
                        {
                            i.Id,
                            i.Title,
                            i.Description,
                            i.CreatedAt,
                            i.CreatedBy,
                            i.Prompt,
                            i.Model,
                            i.Uuid,
                            i.Status,
                            Questions = _context.InterviewQuestion.Where(q=>q.InterviewId==i.Id).ToList(),
                            Invitations = _context.InterviewInvitation.Where(iv=>iv.InterviewId==i.Id).Include("Results").Include("Responses").ToList()
                        }
                    ).ToList();
                    await response.WriteAsJsonAsync(interviews);
                }
                else
                {
                    var interviews = _context.Interview.Where(i => i.CreatedBy.ToLower() == email.ToLower()).ToList();
                    await response.WriteAsJsonAsync(interviews);
                }                             
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.Unauthorized);
            }                        
            return response;
        }
    }
}
