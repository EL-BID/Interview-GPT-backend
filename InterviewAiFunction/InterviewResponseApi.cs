using InterviewAiFunction.Serializers;
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
    public class InterviewResponseApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResponseApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>(); ;
        }

        public InterviewResponseApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResponseApi>();
        }

        [Function("InterviewResponse")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "put", "delete", Route = "response")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            if(req.Method== "GET")
            {
                try
                {
                    string invitationCode = req.Query["InvitationCode"];
                    int responseId = int.Parse(req.Query["Id"]);
                    InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i=>i.InvitationCode == invitationCode);    
                    if(invitation != null)
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
                                if(invitation != null)
                                {
                                    if(responseSerializer.Id != null)
                                    {
                                        InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                                        if(interviewResponse != null)
                                        {
                                            interviewResponse.ResponseText = responseSerializer.ResponseText ?? interviewResponse.ResponseText;
                                            interviewResponse.UpdatedAt = DateTime.Now;
                                            _context.InterviewResponse.Update(interviewResponse);
                                            await _context.SaveChangesAsync();
                                        }
                                        else
                                        {
                                            response = req.CreateResponse(HttpStatusCode.BadRequest);
                                            response.WriteString("Result not found.");
                                        }
                                    }
                                    else
                                    {
                                        //TODO: validate that InterviewQuestionId is in the request;
                                        DateTime now = DateTime.Now;
                                        InterviewResponse interviewResponse = new InterviewResponse
                                        {
                                            CreatedAt = now,
                                            UpdatedAt = now,
                                            ResponseText = responseSerializer.ResponseText,
                                            InterviewInvitationId = invitation.Id,
                                            InterviewQuestionId = (int)responseSerializer.InterviewQuestionId,
                                        };
                                        _context.InterviewResponse.Add(interviewResponse);
                                        await _context.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            // ADMIN ONLY.
                            InterviewResponse interviewResponse = _context.InterviewResponse.Find(responseSerializer.Id);
                            if(interviewResponse != null)
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
