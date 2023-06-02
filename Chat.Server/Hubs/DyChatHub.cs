using System;
using System.Threading.Tasks;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.SignalR;

namespace dymaptic.Chat.Server.Hubs
{
    public class DyChatHub : Hub
    {
        public const string HubUrl = "/chathub";
        public async Task Broadcast(string username, string message)
        {
            Console.WriteLine($"Broadcast initiated for {username}");
            await Clients.All.SendAsync("Broadcast", username, message);
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(ex);
        }
        public async Task SendMessage(ChatMessage message)
        {
            Console.WriteLine($"Received messages for {Context.ConnectionId} " + message.ToString());
            //TODO call AI, do message formatting
            var responseMessage = new ChatMessage("dymaptic", "dymaptic AI is currently not working, please try again later", false);
            await Clients.Client(Context.ConnectionId).SendAsync(ChatHubRoutes.ResponseMessage, responseMessage, Context.ConnectionAborted);
        }
    }
}
