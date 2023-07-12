using dymaptic.ArcGIS.Authentication;
using dymaptic.ArcGIS.Authentication.AppSupport;
using dymaptic.ArcGIS.Authentication.Services;
using dymaptic.Chat.Server.Authentication;
using dymaptic.Chat.Server.Hubs;
using dymaptic.Chat.Server.Logging;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

namespace dymaptic.Chat.Server;
public class Program
{
    public static void Main(string[] args)
    {

        // Entry Point for Server Application
        /* We bootstrap Serilog immediately to catch all errors.
         * Serilog's static Log class should only be used here in Program.cs and only before builder.Build().
         * After that point, app.Logger exists and app.Services can retrieve ILogger<T> instances.
         */
        ServerLogging.Bootstrap();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog(ServerLogging.ConfigureHostUseSerilog);
            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            builder.Services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB
            }); //.AddAzureSignalR();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<AiService>();
            builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection("AiService"));
            builder.Services.AddSingleton<IAppConfigService, DyChatConfigService>();

            builder.Services.AddTransient<IArcGISTokenAuthenticationManager, ArcGISTokenAuthenticationManager>();
            builder.Services.AddTransient<IArcGISTokenClaimBuilder, ArcGISTokenClaimBuilder>();
            builder.Services
                .AddTransient<IOrganizationAuthenticationService, DefaultOrganizationAuthenticationService>();
            builder.Services.AddScoped<IArcGISPortalService, ArcGISPortalService>();

            builder.Services.AddAuthenticationServices(builder.Configuration);
            
            var validOrgIds = builder.Configuration.GetSection("ArcGIS:ValidOrgIds").Get<string[]>();


            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ValidOrganization", policy =>
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c =>
                            (c.Type == ArcGISTokenClaimTypes.ArcGISOrganizationId)
                            && validOrgIds!.Any(x => x.Equals(c.Value)));
                    }));
            });

            var app = builder.Build();

            // Streamlined request logging
            app.UseSerilogRequestLogging(ServerLogging.RequestLogging);

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
            app.MapPost("LogError", (HttpRequest request, Guid? messageId, [FromBody] ErrorMessageRequest errorMessageRequest) =>
            {

                if (errorMessageRequest.ErrorToken == Guid.Parse("AC72107E-9536-4E20-A1B8-B299669399B6"))
                {
                    app.Logger.LogError("There was an error with id {messageId} on the ArcGIS chat client: {exceptionMessage} \r\n {exceptionStackTrack} \r\n {exceptionInnerException}",
                        messageId, errorMessageRequest.ExceptionMessage, errorMessageRequest.ExceptionStackTrack, errorMessageRequest.ExceptionInnerException);
                }
            });

            app.Run();
        }
        catch (Exception ex)
        {
            if (ex.GetType().Name != "StopTheHostException")
            {
                Log.Fatal(ex, "Unhandled Program.cs Exception");
            }
        }
        finally
        {
            Log.Verbose("Application Stopped");
            ServerLogging.Dispose();
        }
    }
}