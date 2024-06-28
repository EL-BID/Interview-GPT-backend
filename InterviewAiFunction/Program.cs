using Azure.Identity;
using InterviewAiFunction;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        string sqlConnection = Environment.GetEnvironmentVariable("sqldb_connection");
        //var mic = new ManagedIdentityCredential();
        //mic.GetToken(new Azure.Core.TokenRequestContext(new string[] { "https://vault.azure.net/.default" }));
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddMvcCore().AddNewtonsoftJson(options =>
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
        services.AddDbContext<InterviewContext>(options=>options.UseSqlServer(sqlConnection));
    })
    .Build();


host.Run();
