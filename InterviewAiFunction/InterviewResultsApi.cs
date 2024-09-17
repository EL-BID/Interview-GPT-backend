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
    public class InterviewResultsApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResultsApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public InterviewResultsApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<PublicInterviewResultApi>();
        }

        [Function("InterviewResultsApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;
            string interviewIdParam = req.Query["InterviewId"];
            string sessionIdParam = req.Query["SessionId"];
            string isAdminParam = req.Query["Admin"];
            try
            {
                int interviewId = int.Parse(interviewIdParam);
                int sessionId = int.Parse(sessionIdParam);
                bool adminRights = false;

                if(isAdminParam!=null && isAdminParam == "true" && dbCommons.IsValidAdminUserForInterview(interviewId, email))
                {
                    adminRights = true;
                }
                var responses = _context.InterviewResult
                        .Join(
                            _context.InterviewSession, ir => ir.SessionId, iss => iss.Id, (ir, iss) => new
                            {
                                ir.Id,
                                ir.SessionId,
                                ir.ResultAi,
                                ir.CreatedAt,
                                ir.UpdatedAt,
                                iss.InterviewId,
                                iss.SessionUser
                            }
                        )
                        .Where(
                            x => x.InterviewId == interviewId && (!adminRights ? x.SessionUser == email : true) && (sessionId != null ? x.SessionId == sessionId : true)
                        ).ToList();
                await response.WriteAsJsonAsync(responses);
            }
            catch (Exception ex)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
            }
            return response;
        }
    }
}
