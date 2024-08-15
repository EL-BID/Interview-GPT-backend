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
    public class PublicInterviewResponseApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public PublicInterviewResponseApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>(); ;
        }

        public PublicInterviewResponseApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<PublicInterviewResponseApi>();
        }

        [Function("PublicInterviewResponseApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "public/response")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            if (req.Method== "GET")
            {
                try
                {
                    string invitationCode = req.Query["InvitationCode"];
                    int responseId = int.Parse(req.Query["Id"]);
                    InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i=>i.InvitationCode == invitationCode);    
                    if(invitation != null && dbCommons.IsValidInvitationForResponse(invitation, responseId))
                    {
                        InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseId);
                        await response.WriteAsJsonAsync(interviewResponse);
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("Result not found.");
                    }
                }catch (Exception ex)
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
                    InterviewResponseSerializer responseSerializer = JsonSerializer.Deserialize<InterviewResponseSerializer>(requestBody);  
                    if(responseSerializer != null)
                    {
                        if (req.Method== "PUT")
                        {
                            if (responseSerializer.InvitationCode != null)
                            {
                                InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == responseSerializer.InvitationCode);

                                /*
                                 TODO:
                                    1. Validate that session id belongs to invited user.
                                 */
                                if(invitation != null && dbCommons.IsValidInvitationForSession(invitation, responseSerializer.SessionId))
                                {
                                    if(responseSerializer.Id != null)
                                    {
                                        // UPDATE
                                        InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                                        if(interviewResponse != null)
                                        {
                                            if(dbCommons.IsValidInvitationForResponse(invitation, interviewResponse.Id))
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
                                            response = req.CreateResponse(HttpStatusCode.BadRequest);
                                            response.WriteString("Result not found.");
                                        }
                                    }
                                    // CREATE
                                    else
                                    {
                                        if(dbCommons.IsInterviewQuestionforInvitation((int)responseSerializer.InterviewQuestionId, invitation))
                                        {
                                            try
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
                                            catch (Exception ex)
                                            {
                                                response = req.CreateResponse(HttpStatusCode.BadRequest);
                                                response.WriteString("Error with sent parameters");
                                            }
                                        }
                                        else
                                        {
                                            response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                        }                                        
                                    }
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == responseSerializer.InvitationCode);
                            InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                            if(interviewResponse != null && dbCommons.IsValidInvitationForResponse(invitation, interviewResponse.Id))
                            {
                                _context.InterviewResponse.Remove(interviewResponse);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                response = req.CreateResponse(HttpStatusCode.NotFound);
                            }
                        }
                    }
                }catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    if (ex is DbUpdateException)
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
