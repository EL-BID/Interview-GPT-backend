using DarkLoop.Azure.Functions.Authorization;
using InterviewAiFunction.Serializers;
using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewCopyApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewCopyApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewCopyApi>();
        }

        public InterviewCopyApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewCopyApi>();
            _context = context;
        }



        [Function("InterviewCopy")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "interviewCopy")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            //TODO admin validation.
            var email = req.Identities.First().Name.ToLower();
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                InterviewSerializer interviewSerializer = JsonSerializer.Deserialize<InterviewSerializer>(requestBody);
                if (interviewSerializer != null)
                {
                    if (interviewSerializer.Id != null && dbCommons.IsUserInterviewId((int)interviewSerializer.Id, email))
                    {
                        Interview interview = _context.Interview.Find(interviewSerializer.Id);
                        if (interview != null && interview.CreatedBy.ToLower() == email)
                        {
                            List<InterviewQuestion> questions = _context.InterviewQuestion.Where(q => q.InterviewId == interview.Id).ToList();
                            Interview interviewCopy = new Interview
                            {
                                CreatedAt = DateTime.Now,
                                CreatedBy = interview.CreatedBy,
                                Title = interview.Title,
                                Description = interview.Description,
                                Model = interview.Model,
                                Prompt = interview.Prompt,
                                Uuid = System.Guid.NewGuid().ToString(),
                                Status = "inactive"
                            };
                            _context.Interview.Add(interviewCopy);
                            InterviewQuestion interviewQuestionCopy;
                            foreach (var question in questions)
                            {
                                interviewQuestionCopy = new InterviewQuestion
                                {
                                    InterviewId = interview.Id,
                                    IsRequired = question.IsRequired,
                                    QuestionText = question.QuestionText,
                                    QuestionOrder = question.QuestionOrder
                                };
                                _context.InterviewQuestion.Add(interviewQuestionCopy);
                            }
                            await _context.SaveChangesAsync();
                            await response.WriteAsJsonAsync(interviewCopy);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.BadRequest);
                            await response.WriteStringAsync("Interview not found.");
                        }
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        await response.WriteStringAsync("Missing Id from request");
                    }
                }
                else
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    await response.WriteStringAsync("Error with arguments.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                response = dbCommons.ProcessDbException(req, ex);                
            }
            return response;
        }
    }
}
