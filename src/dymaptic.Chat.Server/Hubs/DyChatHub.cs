using System.Text;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.SignalR;

namespace dymaptic.Chat.Server.Hubs;

public class DyChatHub : Hub
{
    public DyChatHub(AiService aiService)
    {
        _aiService = aiService;
    }

    public const string HubUrl = "/chathub";
    public async Task Broadcast(DyChatMessage message)
    {
        // this is for talking between chat clients, not currently in use or related to the AI service
        Console.WriteLine($"Broadcast initiated for {message.Username ?? message.SenderType.ToString()}");
        await Clients.All.SendAsync("Broadcast", message);
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

    public async Task SendMessage(DyChatMessage message)
    {
        Console.WriteLine($"Received messages for {Context.ConnectionId} " + message.ToString());
        //TODO call AI, do message formatting
        //var responseMessage = new DyChatMessage("dymaptic", GetRandomHal9000Quote(), false);
        //await Clients.Client(Context.ConnectionId).SendAsync(ChatHubRoutes.ResponseMessage, responseMessage, Context.ConnectionAborted);
    }

    public async IAsyncEnumerable<char> QueryChatService(DyRequest request)
    {
        Console.WriteLine($"Received messages for {Context.ConnectionId} " + request.Messages.Last());
        Stream stream = await _aiService.Query(request);
        // loop through the stream and return a small set of characters at a time
        using var reader = new StreamReader(stream, Encoding.UTF8);
        Memory<char> buffer = new Memory<char>(new char[1]);
        while (!reader.EndOfStream)
        {
            await reader.ReadAsync(buffer);
            yield return buffer.Span[0];
        }
    }

    private readonly AiService _aiService;
}
