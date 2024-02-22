using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace dymaptic.AI.ChatService;

public class ApiKeyAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public List<string> ApiKeys { get; set; }
}
public class ApiKeyAuthenticationHandler: AuthenticationHandler<ApiKeyAuthenticationHandlerOptions>
{
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Check to see if the request has an Authorization header
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                // Check to see if the request has a token query parameter instead
                if (!Request.Query.ContainsKey("token"))
                {
                    return Task.FromResult(
                        AuthenticateResult.Fail("Missing Authorization Header or token query parameter"));
                }
            }

            // Get the token from the Authorization header or the token query parameter
            var token = Request.Headers.ContainsKey("Authorization")
                ? Request.Headers["Authorization"].ToString()
                : Request.Query["token"].ToString();

            // Check to see if the token is in a list from the configuration
            if (!Options.ApiKeys.Contains(token.Split()[1]))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid Authorization Header or token query parameter"));
            }

            // return success
            var claims = new[] {new Claim(ClaimTypes.Name, token)};
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception e)
        {
            return Task.FromResult(AuthenticateResult.Fail(e.Message));
        }
    }
}