using System.Text;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace dymaptic.Chat.Server.Hubs;

[Authorize(Policy = "ValidOrganization")]
public class DyChatHub : Hub
{
    public DyChatHub(AiService aiService)
    {
        _aiService = aiService;
    }


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

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(ex);
    }


    public async IAsyncEnumerable<char> QueryChatService(DyRequest request)
    {
        Console.WriteLine($"Received messages for {Context.ConnectionId} " + request.Messages.Last());
        Stream? stream = default;
        try
        {
            stream = await _aiService.Query(request);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (stream != null)
        {
            // loop through the stream and return a small set of characters at a time
            using var reader = new StreamReader(stream, Encoding.UTF8);
            Memory<char> buffer = new Memory<char>(new char[1]);
            while (!reader.EndOfStream)
            {
                await reader.ReadAsync(buffer);
                yield return buffer.Span[0];
            }
        }
        else
        {
            var errorMessage =
                "Sorry, we are currently unable to process your question, please try again later.".ToCharArray();
            foreach (var c in errorMessage)
            {
                yield return c;
            }
        }
    }

    private readonly AiService _aiService;
}
