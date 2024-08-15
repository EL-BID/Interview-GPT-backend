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
    public class PublicInterviewResultApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public PublicInterviewResultApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewQuestionApi>();
        }

        public PublicInterviewResultApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<PublicInterviewResultApi>();
        }



        [Function("PublicInterviewResultApi")]
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
                    int sessionId = int.Parse(req.Query["SessionId"]);
                    int resultId = req.Query["Id"]==null ? int.Parse(req.Query["Id"]) : -1;
                    InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(r=>r.InvitationCode== invitationCode);
                    if(invitation != null && dbCommons.IsValidInvitationForSession(invitation, sessionId))
                    {
                        InterviewResult result = _context.InterviewResult.OrderByDescending(x=>x.CreatedAt).FirstOrDefault(x=>x.SessionId==sessionId && x.Id==(resultId!=-1 ?resultId:x.Id));
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
                                if(invitation!=null && dbCommons.IsValidInvitationForSession(invitation, resultSerializer.SessionId)){
                                    InterviewResult result = _context.InterviewResult.FirstOrDefault(r => r.Id == resultSerializer.Id);
                                    if (result != null)
                                    {
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
                                            SessionId = resultSerializer.SessionId,
                                            ResultAi = resultSerializer.ResultAi,
                                        };
                                        _context.InterviewResult.Add(result);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(result);
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
                            InterviewResult result = _context.InterviewResult.Find(resultSerializer.Id);
                            InterviewInvitation invitation = _context.InterviewInvitation.FirstOrDefault(i => i.InvitationCode == resultSerializer.InvitationCode);                            
                            if (result != null && dbCommons.IsValidInvitationForResult(invitation, result.Id))
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
