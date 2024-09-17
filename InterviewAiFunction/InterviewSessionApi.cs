using DarkLoop.Azure.Functions.Authorization;
using InterviewAiFunction.Serializers;
using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewSessionApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewSessionApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewSessionApi>();
        }

        public InterviewSessionApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewSessionApi>();
        }

        [Function("InterviewSessionApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "session")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;
            if (req.Method == "GET")
            {
                try
                {
                    
                    int sessionId = int.Parse(req.Query["Id"]);
                    string interviewUuid = req.Query["InterviewUuid"]; //gets all the sessions for an interview.
                    string isAdminParam = req.Query["Admin"];
                    bool adminRights = false;
                    if (isAdminParam != null && isAdminParam == "true" && dbCommons.IsValidAdminUserForInterviewUuid(interviewUuid, email))
                    {
                        adminRights = true;
                    }
                    // TODO: add option for admin where it gets sessions unfiltered by user

                    if (sessionId != null)
                    {
                        if (dbCommons.IsValidUserForSession(sessionId, email) || adminRights)
                        {
                            var session = _context.InterviewSession.FirstOrDefault(x => x.Id == sessionId);
                            await response.WriteAsJsonAsync(session);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.Unauthorized);
                        }
                    }
                    else
                    {
                        // get all the sessions for the use
                        if (dbCommons.IsValidUserForInterviewUuid(interviewUuid, email) || adminRights)
                        {
                            var interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewUuid);
                            var sessions = _context.InterviewSession.Where(x => x.InterviewId == interview.Id && (adminRights? true: x.SessionUser==email)).ToList();
                            await response.WriteAsJsonAsync(sessions);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.NotFound);
                        }
                    }
                }
                catch (Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                }                
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                try
                {
                    InterviewSessionSerializer sessionSerializer =  JsonSerializer.Deserialize<InterviewSessionSerializer>(requestBody);
                    if(sessionSerializer != null)
                    {                        
                        if (req.Method == "PUT")
                        {
                            if(sessionSerializer.Id != null)
                            {
                                InterviewSession session = _context.InterviewSession.Find(sessionSerializer.Id);
                                if(session!=null && dbCommons.IsValidUserForSession(session.Id, email))
                                {
                                    session.Title = sessionSerializer.Title ?? session.Title;
                                    session.Status = sessionSerializer.Status ?? session.Status;
                                    session.Result = sessionSerializer.Result ?? session.Result;
                                    session.UpdatedAt = DateTime.Now;
                                    _context.InterviewSession.Update(session);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(session);
                                }
                            }
                            else
                            {
                                Interview interview = _context.Interview.Find(sessionSerializer.InterviewId);
                                if (dbCommons.IsValidUserForInterview(interview.Id, email))
                                {
                                    InterviewSession session = new InterviewSession
                                    {
                                        Title = sessionSerializer.Title ?? null,
                                        SessionUser = email,
                                        CreatedAt = DateTime.Now,
                                        Result = sessionSerializer.Result ?? null,
                                        Status = "active"
                                    };
                                    _context.InterviewSession.Add(session);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(session);
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            InterviewSession session = _context.InterviewSession.Find(sessionSerializer.Id);
                            if(session != null && dbCommons.IsValidUserForSession(session.Id, email))
                            {
                                _context.InterviewSession.Remove(session);
                                await _context.SaveChangesAsync();
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
                }
                catch(Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
            return response;
        }
    }
}
