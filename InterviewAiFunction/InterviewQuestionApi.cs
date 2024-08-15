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
    public class InterviewQuestionApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewQuestionApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public InterviewQuestionApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>(); ;
        }

        [Function("InterviewQuestion")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route ="question")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;
            if (req.Method=="GET")
            {
                try
                {
                    int questionId = int.Parse(req.Query["Id"]);
                    InterviewQuestion question = _context.InterviewQuestion.Find(questionId);
                    //TODO: validate that the user is the interview owner.
                    if (question != null && dbCommons.IsUserQuestion(question, email))
                    {
                        await response.WriteAsJsonAsync(question);
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("Question not found.");
                    }
                }catch(Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString("Arguments error");
                }                
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                try
                {
                    InterviewQuestionSerializer questionSerializer = JsonSerializer.Deserialize<InterviewQuestionSerializer>(requestBody);
                    if(questionSerializer != null)
                    {
                        if(req.Method=="PUT")
                        {
                            if(questionSerializer.Id != null)
                            {
                                InterviewQuestion question = _context.InterviewQuestion.Find(questionSerializer.Id);
                                if(question != null && dbCommons.IsUserQuestion(question, email))
                                {
                                    question.QuestionOrder = questionSerializer.QuestionOrder ?? question.QuestionOrder;
                                    question.QuestionText = questionSerializer.QuestionText ?? question.QuestionText;
                                    question.IsRequired = questionSerializer.IsRequired ?? question.IsRequired;
                                    _context.InterviewQuestion.Update(question);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(question);
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.NotFound);
                                }
                            }
                            else
                            {
                                if(questionSerializer.InterviewId != null && dbCommons.IsValidAdminUserForInterview((int)questionSerializer.InterviewId, email))
                                {
                                    InterviewQuestion question = new InterviewQuestion
                                    {
                                        QuestionText = questionSerializer.QuestionText,
                                        QuestionOrder = questionSerializer.QuestionOrder ?? 0,
                                        IsRequired = questionSerializer.IsRequired ?? true,
                                        InterviewId = (int) questionSerializer.InterviewId
                                    };
                                    _context.InterviewQuestion.Add(question);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(question);
                                }

                                
                            }
                        }
                    }
                    else if (req.Method=="DELETE")
                    {
                        InterviewQuestion question = _context.InterviewQuestion.Find(questionSerializer.Id);
                        if (question != null && dbCommons.IsUserQuestion(question, email))
                        {
                            _context.InterviewQuestion.Remove(question);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.NotFound);
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    if(ex is DbUpdateException)
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("Error updating the database check values provided.");
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }
            }
            return response;
        }
    }
}
