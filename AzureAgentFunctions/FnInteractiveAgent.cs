using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker.Http;

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
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _logger.LogInformation("User authenticated: {UserId}", req.Identities.FirstOrDefault()?.Name);

        // Create successful response
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        
        var responseData = new
        {
            message = "Welcome to Azure Functions!",
            name = req.FunctionContext.GetHttpContext()?.User?.Identity?.Name,
            isAuthN = req.FunctionContext.GetHttpContext()?.User?.Identity?.IsAuthenticated
        };
        await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(responseData));
        return response;
    }
}