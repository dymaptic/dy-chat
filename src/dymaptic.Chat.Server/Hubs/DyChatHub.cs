using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

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
            _logger.LogTrace($"Broadcast initiated for {message.Username ?? message.SenderType.ToString()}");
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
            _logger.LogTrace($"Client connected: {Context.ConnectionId}");
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
            _logger.LogTrace($"Client disconnected: {Context.ConnectionId}");
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

        _logger.LogTrace($"Received messages for {Context.ConnectionId} " + request.Messages.Last());
        Stream? stream = default;
        try
        {
            stream = await _aiService.Query(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error With QueryChatService querying AI Service");
        }

        if (stream != null)
        {
            // loop through the stream and return a small set of characters at a time
            using var reader = new StreamReader(stream, Encoding.UTF8);
            Memory<char> buffer = new Memory<char>(new char[1]);
            var response = new StringBuilder();
            while (!reader.EndOfStream)
            {
                await reader.ReadAsync(buffer);
                response.Append(buffer.ToString());
                yield return buffer.Span[0];
            }

            try
            {
                _logger.LogInformation($"{JsonSerializer.Serialize(new RequestLog(request, response.ToString()))}");
            }
            catch (Exception e)
            {
                //eat exception
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
            _logger.LogError(errorMessage.ToString(), "Error With QueryChatService returning buffered string");
        }
    }

    private readonly AiService _aiService;
    private readonly ILogger<DyChatHub> _logger;
}

internal record RequestLog(DyRequest request, string response)
{
}
