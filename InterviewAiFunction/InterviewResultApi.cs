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
    public class InterviewResultApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResultApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public InterviewResultApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResultApi>();
        }



        [Function("InterviewResult")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "public/result")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);

            if (req.Method == "GET")
            {
                try
                {
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
                        response.WriteString("Result not found.");
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
                            //TODO: ADMIN ONLY
                            InterviewResult result = _context.InterviewResult.Find(resultSerializer.Id);
                            if(result != null)
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
