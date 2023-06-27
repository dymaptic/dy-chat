using dymaptic.ArcGIS.Authentication;
using dymaptic.ArcGIS.Authentication.AppSupport;
using dymaptic.ArcGIS.Authentication.Services;
using dymaptic.Chat.Server.Authentication;
using dymaptic.Chat.Server.Hubs;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;

namespace dymaptic.Chat.Server;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/octet-stream" });
        });
        builder.Services.AddSignalR(); //.AddAzureSignalR();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<AiService>();
        builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection("AiService"));
        builder.Services.AddSingleton<IAppConfigService, DyChatConfigService>();

        builder.Services.AddTransient<IArcGISTokenAuthenticationManager, ArcGISTokenAuthenticationManager>();
        builder.Services.AddTransient<IArcGISTokenClaimBuilder, ArcGISTokenClaimBuilder>();
        builder.Services.AddTransient<IOrganizationAuthenticationService, DefaultOrganizationAuthenticationService>();
        builder.Services.AddScoped<IArcGISPortalService, ArcGISPortalService>();

        builder.Services.AddAuthenticationServices(builder.Configuration);


        //builder.Services.AddAuthorization(options =>
        //{
        //    options.AddPolicy("ValidOrganization",
        //        policy => policy.RequireClaim(ArcGISTokenClaimTypes.ArcGISOrganizationId, validOrgIds));
        //});
        var validOrgIds = builder.Configuration.GetSection("ArcGIS:ValidOrgIds").Get<string[]>();


        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("ValidOrganization", policy =>
                policy.RequireAssertion(context =>
                {
                    return context.User.HasClaim(c =>
                        (c.Type == ArcGISTokenClaimTypes.ArcGISOrganizationId)
                        && validOrgIds.Any(x => x.Equals(c.Value)));
                }));
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
        app.MapHub<DyChatHub>(ChatHubRoutes.HubUrl);

        app.MapAuthenticationEndPoints();

        app.Run();
    }
}