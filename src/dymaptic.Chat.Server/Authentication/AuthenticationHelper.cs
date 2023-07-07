using AspNet.Security.OAuth.ArcGIS;
using dymaptic.ArcGIS.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace dymaptic.Chat.Server.Authentication;

public static class AuthenticationHelper
{
    public static string LoginUri => "login";
    public static string LogoutUri => "logout";
    public static string ArcGISProLoginUri => "arcgispro-login";

    public static void MapAuthenticationEndPoints(this WebApplication app)
    {
        app.Logger.LogInformation("MapAuthenticationEndPoints");

        app.MapGet(LoginUri,
            async (HttpContext context, string? returnUrl, IAuthenticationSchemeProvider provider) =>
            {
                var properties = new AuthenticationProperties()
                {
                    RedirectUri = returnUrl ?? "/"
                };
                await context.ChallengeAsync(ArcGISAuthenticationDefaults.AuthenticationScheme, properties);
                //we may want to try to capture more login info like the token login
                app.Logger.LogInformation("Logging in via OAuth2 to ArcGIS");

            });

        app.MapGet(ArcGISProLoginUri,
            async (HttpRequest request, HttpContext context, IArcGISTokenClaimBuilder claimBuilder) =>
            {
                try
                {
                    string? token = request.Query["token"];

                    if (!String.IsNullOrEmpty(token))
                    {
                        var result =
                            await claimBuilder.BuildClaimAsync(token, ArcGISTokenConstants.DefaultAuthenticationName);

                        if (result.Succeeded)
                        {

                            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal,
                                result.Ticket.Properties);
                            try
                            {
                                var nameClaim = result.Principal.Claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.Name));
                                var emailClaim = result.Principal.Claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.Email));
                                var arcGISClaim = result.Principal.Claims.FirstOrDefault(x => x.Type.Equals(ArcGISTokenClaimTypes.ArcGISOrganizationId));

                                app.Logger.LogInformation("User:{0} \r\n Email: {1} \r\n ArcGISOrg: {2}", nameClaim?.Value, emailClaim?.Value, arcGISClaim?.Value);
                            }
                            catch (Exception ex)
                            {
                                app.Logger.LogInformation("Error writing user information on login");
                            }

                            return Results.StatusCode(200);
                        }
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ArcGISProLoginUri + " " + ex.Message + " " + ex.InnerException);
                    throw;
                }

                return Results.Unauthorized();
            });

        app.MapGet(LogoutUri, async (HttpContext context, string? returnUrl) =>
        {
            var properties = new AuthenticationProperties()
            {
                RedirectUri = returnUrl ?? "/"
            };
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, properties);
        });
    }

    public static void AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Add Logging For authService and configuration values
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/" + LoginUri;
            options.LogoutPath = "/" + LogoutUri;
        })
        .AddCookie(ArcGISTokenConstants.DefaultAuthenticationName, options =>
        {
            options.LoginPath = "/" + ArcGISProLoginUri;
            options.LogoutPath = "/" + LogoutUri;
        })
        .AddArcGIS(options =>
        {
            options.ClaimActions.MapJsonKey(ArcGISTokenClaimTypes.ArcGISOrganizationId, "orgId");
            options.ClientId = configuration["ArcGIS:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["ArcGIS:ClientSecret"] ?? string.Empty;
        });

    }
}