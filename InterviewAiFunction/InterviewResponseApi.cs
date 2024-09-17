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
    public class InterviewResponseApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResponseApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewResponseApi>(); ;
        }

        public InterviewResponseApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResponseApi>();
        }

        [Function("InterviewResponseApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "response")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;
            if (req.Method== "GET")
            {
                try
                {
                    
                    string interviewUuid = req.Query["InterviewUuid"]; //gets all the sessions for an interview.
                    string isAdminParam = req.Query["Admin"];                    
                    bool adminRights = false;
                    if(isAdminParam!=null && isAdminParam=="true" && dbCommons.IsValidAdminUserForInterviewUuid(interviewUuid, email))
                    {
                        adminRights = true;
                    }

                    if (req.Query["Id"] != null)
                    {
                        int responseId = int.Parse(req.Query["Id"]);
                        //single item
                        if (dbCommons.IsValidUserForResponse(responseId, email) || adminRights)
                        {
                            InterviewResponse interviewResponse = _context.InterviewResponse.FirstOrDefault(x => x.Id == responseId);
                            await response.WriteAsJsonAsync(interviewResponse);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.NotFound);
                        }                        
                    }
                    else
                    {
                        // multiple items
                        if (dbCommons.IsValidUserForInterviewUuid(interviewUuid, email) || adminRights)
                        {
                            Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewUuid);
                            if (interview != null)
                            {
                                int sessionId = int.Parse(req.Query["SessionId"]);
                                // only one session
                                var interviewResponses = _context.InterviewResponse.Join(_context.InterviewSession, ir => ir.SessionId, ses => ses.Id, (ir, ses) => new
                                {
                                    ir.SessionId,
                                    ir.Id,
                                    ses.InterviewId,
                                    ir.InterviewQuestionId,
                                    ir.ResponseText,
                                    ir.CreatedAt,
                                    ir.UpdatedAt
                                }
                               ).Where(x => x.InterviewId == interview.Id && (sessionId!=null? x.SessionId==sessionId: true)).ToList();
                                await response.WriteAsJsonAsync(interviewResponses);
                            }
                            else
                            {
                                response = req.CreateResponse(HttpStatusCode.NotFound);
                            }
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.NotFound);
                        }
                    }
                }catch (Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    await response.WriteStringAsync("Arguments error");
                }
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                try
                {
                    InterviewResponseSerializer responseSerializer = JsonSerializer.Deserialize<InterviewResponseSerializer>(requestBody);  
                    if(responseSerializer != null)
                    {
                        if (req.Method== "PUT")
                        {
                            if (responseSerializer.Id != null)
                            {
                                InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                                if (interviewResponse != null)
                                {
                                    if (dbCommons.IsValidUserForResponse((int)responseSerializer.Id, email))
                                    {
                                        interviewResponse.ResponseText = responseSerializer.ResponseText ?? interviewResponse.ResponseText;
                                        interviewResponse.UpdatedAt = DateTime.Now;
                                        _context.InterviewResponse.Update(interviewResponse);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(interviewResponse);
                                    }
                                    else
                                    {
                                        response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                    }
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.NotFound);
                                }
                            }
                            else {
                                if(dbCommons.IsValidUserForSession(responseSerializer.SessionId, email))
                                {
                                    DateTime now = DateTime.Now;
                                    InterviewResponse interviewResponse = new InterviewResponse
                                    {
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        ResponseText = responseSerializer.ResponseText,
                                        SessionId = responseSerializer.SessionId,
                                        InterviewQuestionId = (int)responseSerializer.InterviewQuestionId,
                                    };
                                    _context.InterviewResponse.Add(interviewResponse);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(interviewResponse);
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                            if(interviewResponse != null)
                            {
                                InterviewSession session = _context.InterviewSession.FirstOrDefault(x => x.Id == interviewResponse.SessionId);
                                if(dbCommons.IsValidUserForResponse((int)responseSerializer.Id, email) || dbCommons.IsValidAdminUserForInterview(session.InterviewId, email))
                                {
                                    _context.InterviewResponse.Remove(interviewResponse);
                                    await _context.SaveChangesAsync();
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }
                                
                            }
                            else
                            {
                                response = req.CreateResponse(HttpStatusCode.NotFound);
                            }
                        }
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    response = dbCommons.ProcessDbException(req, ex);
                }
            }
            return response;
        }
    }
}
