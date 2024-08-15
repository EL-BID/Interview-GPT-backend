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
    public class InterviewResultPublicApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResultPublicApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public InterviewResultPublicApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResultPublicApi>();
        }



        [Function("InterviewResultPublic")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "public/result")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            if (req.Method == "GET")
            {
                try
                {
                    // not validated as it's supposed to be a one-one relation between invitation and result (as of now)
                    string invitationCode = req.Query["InvitationCode"];
                    InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(r=>r.InvitationCode== invitationCode);
                    if(invitation != null)
                    {                        
                        InterviewResult result = _context.InterviewResult.Find(invitation.Id);
                        await response.WriteAsJsonAsync(result);
                    }
                    else
                    {
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        await response.WriteStringAsync("Result not found.");
                    }
                    
                }catch(Exception ex)
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
                    InterviewResultSerializer resultSerializer = JsonSerializer.Deserialize<InterviewResultSerializer>(requestBody);
                    if(resultSerializer != null)
                    {
                        if (req.Method == "PUT")
                        {
                            if (resultSerializer.InvitationCode != null)
                            {
                                InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == resultSerializer.InvitationCode);
                                if(invitation != null)
                                {
                                    InterviewResult result = _context.InterviewResult.FirstOrDefault(r=>r.InterviewInvitationId == invitation.Id);
                                    if(result != null)
                                    {
                                        result.ResultUser = resultSerializer.ResultUser ?? result.ResultUser;
                                        result.ResultAi = resultSerializer.ResultAi ?? result.ResultAi;
                                        result.UpdatedAt = DateTime.Now;
                                        _context.InterviewResult.Update(result);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(result);
                                    }
                                    else
                                    {
                                        DateTime now = DateTime.Now;
                                        result = new InterviewResult
                                        {
                                            CreatedAt = now,
                                            UpdatedAt = now,
                                            InterviewInvitationId = invitation.Id,
                                            ResultAi = resultSerializer.ResultAi,
                                            ResultUser = resultSerializer.ResultUser,
                                        };
                                        _context.InterviewResult.Add(result);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(result);
                                    }
                                }
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            
                            InterviewResult result = _context.InterviewResult.Find(resultSerializer.Id);
                            InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == resultSerializer.InvitationCode);
                            if (result != null && result.InterviewInvitationId==invitation.Id)
                            {
                                _context.InterviewResult.Remove(result);
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
                    response = dbCommons.ProcessDbException(req, ex);
                }

            }

            return response;
        }
    }
}
