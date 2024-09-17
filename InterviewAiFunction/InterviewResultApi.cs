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
    public class InterviewResultApi
    {
        private readonly ILogger _logger;
        private readonly InterviewContext _context;

        public InterviewResultApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InterviewResultApi>();
        }

        public InterviewResultApi(InterviewContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<InterviewResultApi>();
        }



        [Function("InterviewResultApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route = "result")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name;

            if (req.Method == "GET")
            {
                try
                {
                    // not validated as it's supposed to be a one-one relation between invitation and result (as of now)                    
                    int sessionId = int.Parse(req.Query["SessionId"]);
                    int resultId = req.Query["Id"]==null ? int.Parse(req.Query["Id"]) : -1;

                    InterviewResult result = _context.InterviewResult.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Id == resultId);
                    if (result != null)
                    {
                        if(dbCommons.IsValidUserForResult(result, email)) {
                            await response.WriteAsJsonAsync(result);
                        }
                        else
                        {
                            response = req.CreateResponse(HttpStatusCode.Unauthorized);
                        }
                    }
                    else {
                        response = req.CreateResponse(HttpStatusCode.NotFound);
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
                            if (resultSerializer.Id != null)
                            {
                                InterviewResult result = _context.InterviewResult.FirstOrDefault(r => r.Id == resultSerializer.Id);
                                if (result != null)
                                {
                                    if (dbCommons.IsValidUserForResult(result, email))
                                    {
                                        result.ResultAi = resultSerializer.ResultAi ?? result.ResultAi;
                                        result.UpdatedAt = DateTime.Now;
                                        _context.InterviewResult.Update(result);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(result);
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
                            
                            else
                            {
                                if(dbCommons.IsValidUserForSession(resultSerializer.SessionId, email))
                                {
                                    DateTime now = DateTime.Now;
                                    InterviewResult result = new InterviewResult
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
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }                                
                            }
                        }
                        else if (req.Method == "DELETE")
                        {                            
                            InterviewResult result = _context.InterviewResult.Find(resultSerializer.Id);                            
                            if (result != null && dbCommons.IsValidUserForResult(result, email))
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
