using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace AzureAgentFunctions;

public class FnInteractiveAgent
{
    private readonly ILogger<FnInteractiveAgent> _logger;

    public FnInteractiveAgent(ILogger<FnInteractiveAgent> logger)
    {
        _logger = logger;
    }

    [Function("FnInteractiveAgent")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("EchoAuthArtifacts invoked.");
        req.Headers.TryGetValues("Authorization", out var authValues);
        string? authToken = authValues?.FirstOrDefault();
        var context = req.FunctionContext.GetHttpContext();

        // important - authenticate the Azure Function request
        var (authenticationStatus, authenticationResponse) =
            await context.AuthenticateAzureFunctionAsync();
        
        if (!authenticationStatus)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }
        var principal = context.User;
        var details = new
        {
            message = "Easy Auth artifacts echoed.",
            // Tokens (truncated for safety)
            authToken = authToken,
            // Principal basics
            isAuthenticated = principal.Identity.IsAuthenticated,
            name = principal.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            authenticationType = principal.Identity.AuthenticationType,
            
            // Scopes/Roles
            scopes = principal?.Claims.Where(c => c.Type == "scp").Select(c => c.Value).ToArray(),
            roles = principal?.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray(),

            // All claims (type + value) for debugging
            claims = principal?.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        };

        var res = req.CreateResponse(HttpStatusCode.OK);
        res.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await res.WriteStringAsync(JsonSerializer.Serialize(details, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        return res;
    }
}