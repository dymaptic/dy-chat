using AspNet.Security.OAuth.ArcGIS;
using dymaptic.ArcGIS.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace dymaptic.Chat.Server.Authentication;

public static class ApplicationHelper
{
    public static string LoginUri => "/login";
    public static string LogoutUri => "/logout";
    public static string ArcGISProLoginUri => "/arcgispro-login";

    public static void MapAuthenticationEndPoints(this WebApplication app)
    {
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
            async (HttpRequest request, HttpContext context, IArcGISTokenClaimBuilder _claimBuilder) =>
            {
                var token = request.Query["token"];

                var result = await _claimBuilder.BuildClaimAsync(token, ArcGISTokenConstants.DefaultAuthenticationName);

                if (result.Succeeded)
                {
                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal,
                            result.Ticket.Properties);
                    return Results.StatusCode(200);
                }

                return Results.Unauthorized();
                ;
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
            options.LoginPath = LoginUri;
            options.LogoutPath = LogoutUri;
        })
        .AddCookie(ArcGISTokenConstants.DefaultAuthenticationName, options =>
        {
            options.LoginPath = ArcGISProLoginUri;
            options.LogoutPath = LogoutUri;
        })
        .AddArcGIS(options =>
        {
            options.ClaimActions.MapJsonKey(ArcGISTokenClaimTypes.ArcGISOrganizationId, "OrgId");
            options.ClientId = configuration["ArcGIS:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["ArcGIS:ClientSecret"] ?? string.Empty;
        });
    }
}