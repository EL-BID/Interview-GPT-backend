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
            _logger = loggerFactory.CreateLogger<InterviewResultPublicApi>();
        }

        [Function("InterviewResults")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results")] HttpRequestData req)
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
                    var responses = _context.InterviewResult
                        .Join(
                            _context.InterviewInvitation, ir => ir.InterviewInvitationId, ii => ii.Id,
                            (ir, ii) => new
                            {
                                ir.Id,
                                ir.InterviewInvitationId,
                                ir.ResultAi,
                                ir.ResultUser,
                                ir.CreatedAt,
                                ir.UpdatedAt,
                                ii.InterviewId
                            }
                        )
                        .Where(
                            x => x.InterviewId == interviewId
                        ).ToList();
                    await response.WriteAsJsonAsync(responses);
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
