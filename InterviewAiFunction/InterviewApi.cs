using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using InterviewAiFunction.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace InterviewAiFunction
{
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (req.Method == "GET")
            {
                var interviewUuid = req.Query["Uuid"];
                if(interviewUuid != null)
                {
                    Interview interview = _context.Interview.Include("Questions").FirstOrDefault(x=> x.Uuid == interviewUuid);
                    await response.WriteAsJsonAsync(interview);
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
                                Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewSerializer.Uuid);
                                if (interview != null)
                                {
                                    interview.Title = interviewSerializer.Title ?? interview.Title;
                                    interview.Model = interviewSerializer.Model ?? interview.Model;
                                    interview.Description = interviewSerializer?.Description ?? interview.Description;
                                    interview.Prompt = interviewSerializer?.Prompt ?? interview.Prompt;
                                    _context.Interview.Update(interview);
                                    await _context.SaveChangesAsync();
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
                                    CreatedBy = "joseza@iadb.org"
                                };
                                _context.Interview.Add(interview);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else if (req.Method == "DELETE")
                        {
                            Interview interview = _context.Interview.FirstOrDefault(x => x.Uuid == interviewSerializer.Uuid);
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            return response;
        }        
    }
}
