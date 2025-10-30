using System;
using DigitalWorkerWithTools.Authentication;
using DigitalWorkerWithTools.Tools;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitialWorkerWithTools.Agents;
using Azure;

namespace AzureFunctions;

public class FnTeamsChatter
{
    private readonly IConfiguration _configuration;
    private readonly FicTokenCredential _ficTokenCredential;
    private readonly MsGraphTools _msGraphTools;
    private readonly IOptions<AgentIdSettings> _authSettings;
    private readonly ILogger _logger;
    private readonly TelemetryClient _telemetryClient;

    private readonly TeamsChatterAgent _teamsChatterAgent;

    public FnTeamsChatter(ILoggerFactory loggerFactory, TelemetryClient telemetryClient,
        FicTokenCredential ficTokenCredential,
        MsGraphTools msGraphTools,
        IOptions<AgentIdSettings> authSettings, IConfiguration configuration
)
    {
        _logger = loggerFactory.CreateLogger<FnTeamsChatter>();
        _telemetryClient = telemetryClient;
        _ficTokenCredential = ficTokenCredential;
        _msGraphTools = msGraphTools;
        _authSettings = authSettings;
        _configuration = configuration;

        _teamsChatterAgent = new TeamsChatterAgent(
            _msGraphTools,
            new AzureKeyCredential(_configuration["ai_apikey"] ?? ""),
            _authSettings.Value.SysPrompt ?? "",
            _configuration["ai_endpoint"] ?? ""
        );

    }

    [Function("FnTeamsChatter")]
    public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        _telemetryClient.TrackEvent("FnTeamsChatter.Executed", new Dictionary<string, string>
        {
            ["ExecutionTime"] = DateTime.Now.ToString("O"),
            ["FunctionName"] = "FnTeamsChatter"
        });

        try
        {
            var result = await _teamsChatterAgent.RunAsync("Check your teams chats and act as per your instructions.");
            _logger.LogInformation("FnTeamsChatter result: {result}", result);
            _telemetryClient.TrackEvent("FnTeamsChatter.ProcessedTeamsChats", new Dictionary<string, string>
            {
                ["ExecutionTime"] = DateTime.Now.ToString("O"),
                ["FunctionName"] = "FnTeamsChatter",
                ["Result"] = result
            });

            result = await _teamsChatterAgent.RunAsync("Check your e-mails and act as per your instructions.");
            _logger.LogInformation("FnTeamsChatter result: {result}", result);
            _telemetryClient.TrackEvent("FnTeamsChatter.ProcessedEmails", new Dictionary<string, string>
            {
                ["ExecutionTime"] = DateTime.Now.ToString("O"),
                ["FunctionName"] = "FnTeamsChatter",
                ["Result"] = result
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FnTeamsChatter execution");

            // Track exception (this is automatically done, but you can add custom properties)
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["FunctionName"] = "FnTeamsChatter",
                ["ExecutionTime"] = DateTime.Now.ToString("O"),
                ["ErrorMessage"] = ex.Message
            });
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);

            // Track metric
            _telemetryClient.TrackMetric("FnTeamsChatter.ScheduleDelay",
                (myTimer.ScheduleStatus.Next - DateTime.Now).TotalMinutes);
        }

        // Track successful execution
        _telemetryClient.TrackEvent("FnTeamsChatter.Success");
    }
}