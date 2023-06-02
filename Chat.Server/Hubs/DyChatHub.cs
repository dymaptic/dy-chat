
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
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
