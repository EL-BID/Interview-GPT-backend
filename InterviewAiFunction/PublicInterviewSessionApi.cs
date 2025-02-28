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
    public class PublicInterviewSessionApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public PublicInterviewSessionApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PublicInterviewSessionApi>();
        }

        public PublicInterviewSessionApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<PublicInterviewSessionApi>();
        }

        [Function("PublicInterviewSessionApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "public/session")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            if (req.Method == "GET")
            {
                try
                {
                    string invitationCode = req.Query["InvitationCode"];                    
                    string interviewUuid = req.Query["InterviewUuid"]; //gets all the sessions for an interview.
                    InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == invitationCode);
                    if (req.Query["Id"] != null)
                    {
                        int sessionId = int.Parse(req.Query["Id"]);
                        if (dbCommons.IsValidInvitationForSession(invitation, sessionId))
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
                        if (dbCommons.IsValidInvitationForInterview(invitation, interviewUuid))
                        {
                            var interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewUuid);
                            var sessions = _context.InterviewSession.Where(x => x.InterviewId == interview.Id).ToList();
                            await response.WriteAsJsonAsync(sessions);
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
                        InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(x=>x.InvitationCode==sessionSerializer.InvitationCode);
                        if (req.Method == "PUT")
                        {
                            if(sessionSerializer.Id != null)
                            {
                                InterviewSession session = _context.InterviewSession.Find(sessionSerializer.Id);
                                if(session!=null && dbCommons.IsValidInvitationForSession(invitation, session.Id))
                                {
                                    session.Title = sessionSerializer.Title ?? session.Title;
                                    session.Status = sessionSerializer.Status ?? session.Status;
                                    session.Result = sessionSerializer.Result ?? session.Result;
                                    session.UserRating = sessionSerializer.UserRating ?? session.UserRating;
                                    session.UpdatedAt = DateTime.Now;
                                    session.CustomInstructions = sessionSerializer.CustomInstructions ?? session.CustomInstructions;
                                    _context.InterviewSession.Update(session);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(session);
                                }
                            }
                            else
                            {
                                Interview interview = _context.Interview.Find(sessionSerializer.InterviewId);
                                if(dbCommons.IsValidInvitationForInterview(invitation, interview.Uuid))
                                {
                                    InterviewSession session = new InterviewSession
                                    {
                                        Title = sessionSerializer.Title ?? null,
                                        SessionUser = invitation.Email,
                                        CreatedAt = DateTime.Now,
                                        Result = sessionSerializer.Result ?? null,
                                        Status = "active",
                                        UserRating = sessionSerializer.UserRating ?? null
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
                            if(session != null && dbCommons.IsValidInvitationForSession(invitation, session.Id))
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
