using Azure;
using DarkLoop.Azure.Functions.Authorization;
using InterviewAiFunction.Serializers;
using InterviewAiFunction.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection.Emit;
using System.Text.Json;

namespace InterviewAiFunction
{
    [FunctionAuthorize]
    public class InterviewTagsApi
    {
        private readonly ILogger<InterviewTagsApi> _logger;
        private readonly InterviewContext _context;

        public InterviewTagsApi(ILogger<InterviewTagsApi> logger)
        {
            _logger = logger;
        }

        public InterviewTagsApi(InterviewContext context, ILogger<InterviewTagsApi> logger)
        {
            _logger = logger;
            _context = context;
        }

        [Function("InterviewTagsApi")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", Route ="tags")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            DatabaseCommons dbCommons = new DatabaseCommons(_context);
            var email = req.Identities.First().Name.ToLower();
            if (req.Method == "GET")
            {
                // Gets all the tags for a provided interview.
                try
                {
                    string interviewUuid = req.Query["interviewUuid"];
                    var interview = _context.Interview.FirstOrDefault(x=>x.Uuid == interviewUuid);
                    if (interview != null)
                    {
                        if(dbCommons.IsValidUserForInterview(interview.Id, email) || dbCommons.IsValidAdminUserForInterview(interview.Id, email))
                        {
                            var tags = _context.InterviewTag.Where(x => x.InterviewId == interview.Id);
                            await response.WriteAsJsonAsync(tags);
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
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                InterviewTagSerializer tagSerializer = JsonSerializer.Deserialize<InterviewTagSerializer>(requestBody);
                if(tagSerializer != null)
                {
                    Interview interview;
                    if(tagSerializer.InterviewId != null)
                    {
                        interview = _context.Interview.Find(tagSerializer.InterviewId);
                    }
                    else
                    {
                        interview = _context.Interview.FirstOrDefault(x => x.Uuid == tagSerializer.InterviewUuid);
                    }
                    if (req.Method == "PUT")
                    {                        
                        if (tagSerializer.Id != null)
                        {
                            InterviewTag tag = _context.InterviewTag.Find(tagSerializer.Id);
                            if (tag != null && dbCommons.IsValidAdminUserForInterview(tag.InterviewId, email))
                            {
                                if (tagSerializer.Label != null)
                                {
                                    tag.Label = tagSerializer.Label ?? tag.Label;
                                    _context.InterviewTag.Update(tag);
                                    await _context.SaveChangesAsync();
                                    await response.WriteAsJsonAsync(tag);
                                }
                                else
                                {

                                }
                            }
                        }
                        else
                        {
                            if (interview != null)
                            {
                                if(dbCommons.IsValidAdminUserForInterview(interview.Id, email))
                                {
                                    if (tagSerializer.Label != null)
                                    {
                                        InterviewTag tag = new InterviewTag
                                        {
                                            InterviewId = interview.Id,
                                            Label = tagSerializer.Label
                                        };
                                        _context.InterviewTag.Add(tag);
                                        await _context.SaveChangesAsync();
                                        await response.WriteAsJsonAsync(tag);
                                    }
                                    else
                                    {
                                        var labels = tagSerializer.Labels;
                                        if(labels != null)
                                        {
                                            _context.InterviewTag.RemoveRange(_context.InterviewTag.Where(x => x.InterviewId == interview.Id));
                                            await _context.SaveChangesAsync();
                                            foreach (var label in labels)
                                            {
                                                _context.InterviewTag.Add(new InterviewTag
                                                {
                                                    InterviewId = interview.Id,
                                                    Label = label
                                                });
                                                await _context.SaveChangesAsync();
                                            }
                                        }
                                        else
                                        {
                                            req.CreateResponse(HttpStatusCode.BadRequest);
                                        }
                                    }
                                    
                                }
                                else
                                {
                                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                                }
                            }
                            else
                            {
                                response = req.CreateResponse(HttpStatusCode.BadRequest);
                            }
                        }
                    }
                    else if (req.Method == "DELETE")
                    {
                       InterviewTag tag = _context.InterviewTag.Find(tagSerializer.Id);
                        if(tag!=null && dbCommons.IsValidAdminUserForInterview(tag.InterviewId, email)){
                            _context.InterviewTag.Remove(tag);
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
                    req.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
            return response;
        }
    }
}
