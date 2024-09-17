using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using DarkLoop.Azure.Functions.Authorization;
using InterviewAiFunction.Serializers;
using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewApi>();
        }

        public InterviewApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewApi>();
        }

        [Function("Interview")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "interview")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var email = "";
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            if (!req.Identities.Any())
            {
                response = req.CreateResponse(HttpStatusCode.Unauthorized);
                return response;
            }
            else
            {
                email = req.Identities.First().Name.ToLower();
            }
            if (req.Method == "GET")
            {
                var interviewUuid = req.Query["Uuid"];
                var adminOption = req.Query["Admin"];


                if(interviewUuid != null)
                {
                    if(adminOption != null && adminOption == "true")
                    {
                        //Interview interview = _context.Interview.Include("Questions").Include("Invitations").Include("Invitations.Responses").Include("Invitations.Results").FirstOrDefault(x => x.Uuid == interviewUuid && x.CreatedBy == email);
                        Interview interview = _context.Interview.Include("Questions").Include("Invitations").FirstOrDefault(x => x.Uuid == interviewUuid && x.CreatedBy == email);
                        await response.WriteAsJsonAsync(interview);
                    }
                    else
                    {
                        Interview interview = _context.Interview.Include("Questions").FirstOrDefault(x=>x.Uuid == interviewUuid);
                        if (interview != null)
                        {
                            bool returnInterview = false;
                            if (interview.InvitationOnly)
                            {
                                if(_context.InterviewInvitation.Any(x=>x.InterviewId == interview.Id && x.Email == email))
                                {
                                    returnInterview = true;
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }
                            }
                            else
                            {
                                returnInterview = true;
                            }
                            if (returnInterview)
                            {
                                InterviewSession currentSession = _context.InterviewSession.FirstOrDefault(x => x.SessionUser == email && x.Status == "active" && x.InterviewId == interview.Id);
                                if (currentSession == null)
                                {
                                    currentSession = new InterviewSession
                                    {
                                        InterviewId = interview.Id,
                                        SessionUser = email,
                                        Status = "active",
                                        CreatedAt = DateTime.Now,
                                    };
                                    _context.InterviewSession.Add(currentSession);
                                    await _context.SaveChangesAsync();

                                }
                                await response.WriteAsJsonAsync(new
                                {
                                    interview = new
                                    {
                                        interview.Id,
                                        interview.Description,
                                        interview.Title,
                                        interview.Prompt,
                                        interview.Status,
                                        interview.Questions
                                    },
                                    session = currentSession
                                });
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
                    response = req.CreateResponse(HttpStatusCode.NotFound);
                }
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                try
                {
                    InterviewSerializer interviewSerializer = JsonSerializer.Deserialize<InterviewSerializer>(requestBody);
                    if (interviewSerializer != null)
                    {

                        if (req.Method == "PUT")
                        {
                            if (interviewSerializer.Uuid != null)
                            {
                                // assumes is a request for update
                                Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewSerializer.Uuid && x.CreatedBy.ToLower()==email);
                                if (interview != null)
                                {
                                    interview.Title = interviewSerializer.Title ?? interview.Title;
                                    interview.Model = interviewSerializer.Model ?? interview.Model;
                                    interview.Description = interviewSerializer?.Description ?? interview.Description;
                                    interview.Prompt = interviewSerializer?.Prompt ?? interview.Prompt;
                                    interview.Status = interviewSerializer?.Status ?? interview.Status;
                                    interview.AuthOnly = interviewSerializer?.AuthOnly ?? interview.AuthOnly;
                                    interview.InvitationOnly = interviewSerializer?.InvitationOnly ?? interview.InvitationOnly;
                                    _context.Interview.Update(interview);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(interview);
                                }
                                else
                                {
                                    // interview not found.
                                    response = req.CreateResponse(HttpStatusCode.NotFound);
                                }
                            }
                            else
                            {
                                // is an insert.
                                Interview interview = new Interview
                                {
                                    Title = interviewSerializer.Title ?? "Untitled",
                                    Uuid = System.Guid.NewGuid().ToString(),
                                    CreatedAt = DateTime.Now,
                                    CreatedBy = email,
                                    Status = "inactive",
                                    Model = interviewSerializer.Model ?? null,
                                    Description = interviewSerializer.Description ?? null,
                                    Prompt = interviewSerializer.Prompt ?? null,
                                    AuthOnly = interviewSerializer.AuthOnly ?? false,
                                    InvitationOnly = interviewSerializer.InvitationOnly ?? false,
                                };
                                _context.Interview.Add(interview);
                                await _context.SaveChangesAsync();
                                await response.WriteAsJsonAsync(interview);

                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewSerializer.Uuid && x.CreatedBy.ToLower() == email);
                            if (interview != null)
                            {
                                _context.Interview.Remove(interview);
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
                catch (Exception ex)
                {
                    //response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    //return response;
                    _logger.LogError(ex.Message);
                    response = dbCommons.ProcessDbException(req, ex);
                    return response;                    
                }
            }
            return response;
        }     
    }
}
