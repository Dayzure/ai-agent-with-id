using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AzureAgentFunctions.EasyAuthHelpers;
public sealed class ClientPrincipal
{
    public string? AuthType { get; set; }
    public List<ClientPrincipalClaim> Claims { get; set; } = new();
    public string? NameType { get; set; }
    public string? RoleType { get; set; }
}

public sealed class ClientPrincipalClaim
{
    public string Typ { get; set; } = default!;
    public string Val { get; set; } = default!;
}

public static class EasyAuthPrincipal
{
    /// <summary>
    /// Parses the X-MS-CLIENT-PRINCIPAL header (base64 JSON) into a ClaimsPrincipal.
    /// Returns null if header is absent or malformed.
    /// </summary>
    public static ClaimsPrincipal? FromBase64Header(string? base64Json)
    {
        if (string.IsNullOrWhiteSpace(base64Json)) return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Json));
            var cp = JsonSerializer.Deserialize<ClientPrincipal>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (cp is null) return null;

            var claims = cp.Claims?.Select(c => new Claim(c.Typ, c.Val)) ?? Enumerable.Empty<Claim>();
            var identity = new ClaimsIdentity(claims, cp.AuthType ?? "EasyAuth",
                                              cp.NameType ?? ClaimTypes.Name,
                                              cp.RoleType ?? ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
