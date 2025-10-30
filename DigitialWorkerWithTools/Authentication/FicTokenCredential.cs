using System.Net.Http.Headers;
using Azure.Core;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Security.Claims;
using DigitialWorkerWithTools.Authentication;
using Microsoft.Identity.Client;
using Azure.Identity;

namespace DigitalWorkerWithTools.Authentication;

public class FicTokenCredential : TokenCredential
{
    private readonly AgentIdSettings _agentIdSettings;
    private readonly AzureAdSettings _azureAdSettings;
    private string[] _scopes = new[] { "api://AzureADTokenExchange/.default" };
    private IConfidentialClientApplication _app;
    private IConfidentialClientApplication _agentIdApp;
    private readonly HttpClient _httpClient = new HttpClient();

    public FicTokenCredential(IOptions<AgentIdSettings> agentIdSettings, IOptions<AzureAdSettings> azureAdSettings)
    {
        _agentIdSettings = agentIdSettings.Value;
        _azureAdSettings = azureAdSettings.Value;

        // Build the confidential client application for the Blueprint
        if (string.IsNullOrEmpty(_azureAdSettings.ClientSecret))
        {
            _app = ConfidentialClientApplicationBuilder.Create(_azureAdSettings.ClientId)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_azureAdSettings.TenantId}"))
            .WithClientAssertion(async (AssertionRequestOptions options) =>
            {
                return await GetMiAssertionForBlueprint();
            })
            .Build();
        }
        else
        {
            _app = ConfidentialClientApplicationBuilder.Create(_azureAdSettings.ClientId)
                .WithClientSecret(_azureAdSettings.ClientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_azureAdSettings.TenantId}"))
                .Build();
        }
        // Build the confidential client application for the Agent Id
        _agentIdApp = ConfidentialClientApplicationBuilder.Create(_agentIdSettings.AgentClientId)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_azureAdSettings.TenantId}"))
            .WithClientAssertion(async (AssertionRequestOptions options) =>
            {
                return await GetBlueprintAssertion();
            })
            .Build();
        // // Specify your user-assigned managed identity client ID
        // var credential = new ManagedIdentityCredential(clientId: "<your-user-assigned-managed-identity-client-id>");

        // // Request token for Microsoft Graph or other resource
        // var tokenRequestContext = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
        // AccessToken token = await credential.WithFmiPath(_authSettings.AgentClientId).GetTokenAsync(tokenRequestContext);

    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {

        // Your custom logic to get the token synchronously
        string token = GetMyToken();
        return new AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(57)); 
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        // Your custom logic to get the token asynchronously
        string token = await GetMyTokenAsync();
        return new AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(57));
    }

    private string GetMyToken()
    {
        return GetAgentUserTokenAsync().Result;
    }

    private async Task<string> GetMyTokenAsync()
    {
        try
        {
            // Acquire token for client credentials flow
            return await GetAgentUserTokenAsync();
        }
        catch (Exception)
        {
            throw;
        }

    }

    public async Task<string> GetAgentUserTokenAsync()
    {

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://login.microsoftonline.com/{_azureAdSettings.TenantId}/oauth2/v2.0/token");

        string blueprintFicToken = await GetBlueprintAssertion();
        string agentIdFicToken = await GetAgentIdToken(new[] { "api://AzureADTokenExchange/.default" });

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "user_fic"),
            new KeyValuePair<string, string>("client_id", _agentIdSettings.AgentClientId),
            new KeyValuePair<string, string>("client_assertion", blueprintFicToken),
            new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
            new KeyValuePair<string, string>("requested_token_use", "on_behalf_of"),
            new KeyValuePair<string, string>("user_federated_identity_credential", agentIdFicToken),
            new KeyValuePair<string, string>("username", _agentIdSettings.AgentUsername),
        });

        return await GetAgenticTokenWithRequest(content);
    }

    private async Task<string> GetBlueprintAssertion()
    {
        var result = await _app.AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
            .WithFmiPath(_agentIdSettings.AgentClientId)
            .ExecuteAsync();
        return result.AccessToken;
    }

    private async Task<string> GetMiAssertionForBlueprint()
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = _azureAdSettings.MiClientId
        });

        // Example: Get token for Microsoft Graph
        var token = credential.GetToken(
            new TokenRequestContext(_scopes)
        );
        string accessToken = token.Token;
        return accessToken;
    }

    private async Task<string> GetAgentIdToken(string[]? scopes = null)
    {
        if (null == scopes)
        {
            scopes = _scopes;
        }

        AuthenticationResult result = await _agentIdApp.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
    
    private async Task<string> GetAgenticTokenWithRequest(FormUrlEncodedContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://login.microsoftonline.com/{_azureAdSettings.TenantId}/oauth2/v2.0/token");

        request.Content = content;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        dynamic? tokenResponse = JsonConvert.DeserializeObject(json);
        return tokenResponse?.access_token ?? throw new InvalidOperationException("Failed to retrieve access token");
    }
}