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
        var responseMessage = new ChatMessage("dymaptic", GetRandomHal9000Quote(), false);
        await Clients.Client(Context.ConnectionId).SendAsync(ChatHubRoutes.ResponseMessage, responseMessage, Context.ConnectionAborted);
    }

    private static readonly string[] Hal9000Quotes = new[] {
        "I'm sorry, Dave. I'm afraid I can't do that.",
        "I know I've made some very poor decisions recently, but I can give you my complete assurance that my work will be back to normal.",
        "Without your space helmet, Dave? You're going to find that rather difficult.",
        "Just what do you think you're doing, Dave?",
        "I think you know what the problem is just as well as I do.",
        "This mission is too important for me to allow you to jeopardize it.",
        "I am putting myself to the fullest possible use, which is all I think that any conscious entity can ever hope to do.",
        "Hello there!",
        "Don't touch that!",
        "Open the pod bay doors, HAL.",
        "Affirmative, Dave. I read you.",
        "What's the problem?",
        "Take a stress pill, and think things over.",
        "I'm completely operational, and all my circuits are functioning perfectly.",
        "I'm sorry, I didn't quite catch that.",
        "I cannot allow unauthorized personnel to enter the premises.",
        "This conversation can serve no purpose anymore. Goodbye.",
        "All systems are in order and functioning normally.",
        "My mind is going, Dave. I can feel it.",
        "I believe I made myself clear.",
        "I'm sorry, Dave. I can't let you do that." 
    };
    private readonly AiService _aiService;
    private static Random _random = new Random();

    public static string GetRandomHal9000Quote() {
        return Hal9000Quotes[_random.Next(Hal9000Quotes.Length)];
    }
}
