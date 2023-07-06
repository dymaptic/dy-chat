using System.Text;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace dymaptic.Chat.Server.Hubs;

[Authorize(Policy = "ValidOrganization")]
public class DyChatHub : Hub
{
    public DyChatHub(AiService aiService, [FromServices] ILogger<DyChatHub> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }


    public async Task Broadcast(DyChatMessage message)
    {

        try
        {
            // this is for talking between chat clients, not currently in use or related to the AI service
            Console.WriteLine($"Broadcast initiated for {message.Username ?? message.SenderType.ToString()}");
            await Clients.All.SendAsync("Broadcast", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error With Broadcast data");
        }
    }

    public override Task OnConnectedAsync()
    {
        try
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error With OnConnectedAsync");
            throw;
        }
        
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        try
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(ex);
        }
        catch 
        {
            _logger.LogError(ex, "Error With OnDisconnectedAsync");
            throw;
        }
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
            _logger.LogError(errorMessage.ToString(), "Error With QueryChatService");
        }
    }

    private readonly AiService _aiService;
    private readonly ILogger<DyChatHub> _logger;
}
