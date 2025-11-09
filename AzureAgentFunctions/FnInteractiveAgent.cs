using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker.Http;
using AzureAgentFunctions.EasyAuthHelpers;
using System.Text.Json;

namespace AzureAgentFunctions;

public class FnInteractiveAgent
{
    private readonly ILogger<FnInteractiveAgent> _logger;

    public FnInteractiveAgent(ILogger<FnInteractiveAgent> logger)
    {
        _logger = logger;
    }

    [Function("FnInteractiveAgent")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
 
       _logger.LogInformation("EchoAuthArtifacts invoked.");

        // 1) Get original tokens (if Easy Auth is configured to provide them)
        //    Common header names for Entra ID; add others if you support more providers.
        req.Headers.TryGetValues("X-MS-TOKEN-AAD-ACCESS-TOKEN", out var accessTokenValues);
        req.Headers.TryGetValues("X-MS-TOKEN-AAD-ID-TOKEN", out var idTokenValues);
        req.Headers.TryGetValues("Authorization", out var authValues);

        string? accessToken = accessTokenValues?.FirstOrDefault();
        string? idToken = idTokenValues?.FirstOrDefault();
        string? authToken = authValues?.FirstOrDefault();

        // 2) Get the Easy Auth client principal header and convert to ClaimsPrincipal
        req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL", out var principalHeaderValues);
        var principalHeader = principalHeaderValues?.FirstOrDefault();

        var principal = EasyAuthPrincipal.FromBase64Header(principalHeader);

        // 3) Prepare a safe response (truncate tokens so we don't echo secrets in full)
        string Truncate(string? s, int keep = 48)
            => string.IsNullOrEmpty(s) ? null! : (s.Length <= keep ? s : s.Substring(0, keep) + "...");

        var details = new
        {
            message = "Easy Auth artifacts echoed.",
            // Tokens (truncated for safety)
            accessTokenStartsWith = Truncate(accessToken),
            idTokenStartsWith = Truncate(idToken),
            authTokenStartsWith = Truncate(authToken),
            // Principal basics
            isAuthenticated = principal?.Identity?.IsAuthenticated ?? false,
            name = principal?.Identity?.Name,
            authenticationType = principal?.Identity?.AuthenticationType,

            // Useful identifiers (if present)
            oid = principal?.Claims.FirstOrDefault(c => c.Type == "oid")?.Value,
            tid = principal?.Claims.FirstOrDefault(c => c.Type == "tid")?.Value,
            sub = principal?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,

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