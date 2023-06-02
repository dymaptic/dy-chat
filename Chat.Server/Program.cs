using dymaptic.Chat.Server.Business;
using dymaptic.Chat.Server.Data;
using dymaptic.Chat.Server;
using dymaptic.Chat.Server.Hubs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;



namespace dymaptic.Chat.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<DyChatMessageHandler>();
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            builder.Services.AddSignalR(); //.AddAzureSignalR();

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

            app.MapBlazorHub();
            app.MapHub<DyChatHub>(DyChatHub.HubUrl);
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}