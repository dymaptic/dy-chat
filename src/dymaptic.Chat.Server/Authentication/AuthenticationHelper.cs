using AspNet.Security.OAuth.ArcGIS;
using dymaptic.ArcGIS.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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

                            return Results.StatusCode(200);
                        }
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "{ArcGISProLoginUri} {message} {innerException}", ArcGISProLoginUri, ex.Message, ex.InnerException);
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