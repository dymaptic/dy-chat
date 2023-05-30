using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Dy_Chat
{
    public class DyChatHub : Hub
    {
        public const string HubUrl = "/dy-chat-hub";
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
    }
}
