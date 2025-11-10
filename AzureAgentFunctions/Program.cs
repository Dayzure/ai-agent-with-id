using DigitalWorkerWithTools.Authentication;
using DigitalWorkerWithTools.Tools;
using DigitialWorkerWithTools.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // Sets up the isolated worker pipeline   
    .ConfigureAppConfiguration(config =>
    {
        // Optional: Add additional configuration sources
        config.AddJsonFile("local.appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddMicrosoftIdentityWebApiAuthentication(context.Configuration, "AzureAD");       
        services.AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .AddSingleton<FicTokenCredential>()
            .AddSingleton(serviceProvider =>
            {
                var tokenCredential = serviceProvider.GetRequiredService<FicTokenCredential>();
                return new MsGraphTools(tokenCredential);
            })
            .Configure<AgentIdSettings>(context.Configuration.GetSection("AgentId"))
            .Configure<AzureAdSettings>(context.Configuration.GetSection("AzureAD"));
    })
    .Build();
host.Run();
