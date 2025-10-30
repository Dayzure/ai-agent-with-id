using DigitalWorkerWithTools.Authentication;
using DigitalWorkerWithTools.Tools;
using DigitialWorkerWithTools.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<FicTokenCredential>()
    .AddSingleton(serviceProvider =>
        {
            var tokenCredential = serviceProvider.GetRequiredService<FicTokenCredential>();
            return new MsGraphTools(tokenCredential);
        })
    .Configure<AgentIdSettings>(builder.Configuration.GetSection("AgentId"))
    .Configure<AzureAdSettings>(builder.Configuration.GetSection("AzureAD"));

builder.Build().Run();
