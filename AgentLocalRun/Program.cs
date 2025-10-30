using System.Security.Claims;
using Azure;
using DigitalWorkerWithTools.Authentication;
using DigitalWorkerWithTools.Tools;
using DigitialWorkerWithTools.Agents;
using DigitialWorkerWithTools.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            .SetBasePath("C:\\demos\\AI\\agent-framework-agentid\\AgentLocalRun")
            .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>()  // Add User Secrets support
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.Configure<AgentIdSettings>(configuration.GetSection("AgentId"));

        // Bind MicrosoftIdentityOptions manually
        services.Configure<AzureAdSettings>(configuration.GetSection("AzureAD"));

        services.AddSingleton<FicTokenCredential>();
        services.AddSingleton(serviceProvider =>
        {
            var tokenCredential = serviceProvider.GetRequiredService<FicTokenCredential>();
            return new MsGraphTools(tokenCredential);
        });

        var serviceProvider = services.BuildServiceProvider();

        //var tokenCredential = serviceProvider.GetRequiredService<FicTokenCredential>();
        var tools = serviceProvider.GetRequiredService<MsGraphTools>();
        var agentCredential = new AzureKeyCredential("");
        TeamsChatterAgent agent = new TeamsChatterAgent(tools, agentCredential);

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var response = await agent.RunAsync(userInput ?? "");
            Console.WriteLine($"Agent: {response}");
        }
    }
}