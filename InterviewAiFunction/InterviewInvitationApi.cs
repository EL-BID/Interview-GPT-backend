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
using System.Security.Cryptography;
using System.Text.Json;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewInvitationApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewInvitationApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        public InterviewInvitationApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewInvitationApi>();
        }

        private string RandomCode(int length)
        {
            string randomAlphanumericString = RandomNumberGenerator.GetString(
                choices: "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
                length: length
            );

            return randomAlphanumericString;
        }

        [Function("InterviewInvitation")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "invitation")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var email = req.Identities.First().Name;
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            if(req.Method=="GET")
            {
                try
                {
                    int invitationId  = int.Parse(req.Query["Id"]);
                    {
                        InterviewInvitation invitation = _context.InterviewInvitation.Find(invitationId);
                        if (invitation != null && dbCommons.IsUserInvitation(invitation, email))
                        {
                            await response.WriteAsJsonAsync(invitation);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.BadRequest);
                            response.WriteString("Invitation not found.");
                        }
                    }
                }
                catch(Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString("Error with arguments");
                }                
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                try
                {
                    InterviewInvitationSerializer invitationSerializer = JsonSerializer.Deserialize<InterviewInvitationSerializer>(requestBody);
                    if (invitationSerializer != null)
                    {
                        if (req.Method == "PUT")
                        {
                            if (invitationSerializer.Id != null)
                            {
                                InterviewInvitation invitation = _context.InterviewInvitation.Find(invitationSerializer.Id);
                                if (invitation != null && dbCommons.IsUserInvitation(invitation, email))
                                {
                                    invitation.InvitationStatus = invitationSerializer.InvitationStatus ?? invitation.InvitationStatus;
                                    _context.InterviewInvitation.Update(invitation);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(invitation);
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.NotFound);
                                }
                            }
                            else
                            {
                                if (invitationSerializer.InterviewId != null && dbCommons.IsValidAdminUserForInterview((int)invitationSerializer.InterviewId, email))
                                {
                                    InterviewInvitation invitation = new InterviewInvitation
                                    {
                                        InterviewId = (int)invitationSerializer.InterviewId,
                                        Email = invitationSerializer.Email,
                                        InvitationStatus = "pending",
                                        CreatedAt = DateTime.Now,
                                        InvitationCode = this.RandomCode(4)+"-"+this.RandomCode(4)+"-"+this.RandomCode(4),
                                    };
                                    _context.InterviewInvitation.Add(invitation);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(invitation);
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            InterviewInvitation invitation = _context.InterviewInvitation.Find(invitationSerializer.Id);
                            if (invitation != null && dbCommons.IsUserInvitation(invitation, email))
                            {
                                _context.InterviewInvitation.Remove(invitation);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                response = req.CreateResponse(HttpStatusCode.NotFound);
                            }
                        }
                    }
                }
                catch(Exception ex)
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
