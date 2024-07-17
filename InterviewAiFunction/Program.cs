using Azure.Identity;
using InterviewAiFunction;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    //.ConfigureFunctionsWorkerDefaults()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseFunctionsAuthorization();
    })
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
        services.AddFunctionsAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //https://gist.github.com/bachoang/90b646e2fedb0a446522d5e0076dddf7#file-startup-cs-L30
        .AddJwtFunctionsBearer(options =>
        {
            options.Authority = Environment.GetEnvironmentVariable("jwt_authority");
            options.Audience = Environment.GetEnvironmentVariable("jwt_audience");
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidAudiences = new List<string> { Environment.GetEnvironmentVariable("jwt_audience")},
                ValidIssuers = new List<string> { Environment.GetEnvironmentVariable("jwt_issuer"), Environment.GetEnvironmentVariable("jwt_issuer")+"v2.0" }
            };

        });
        services.AddFunctionsAuthorization(options =>
        {
            options.AddPolicy("OnlyAdmins", policy => policy.RequireRole("Admin"));
            //options.AddPolicy("UserWrite", policy => policy.RequireClaim(""));
        });
    })
    .Build();


host.Run();
