using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace dymaptic.Chat.Server.Business
{
    public class DyChatMessageHandler
    {
        public DyChatMessageHandler(IConfiguration configuration, ILogger<DyChatMessageHandler> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Chat()
        {
            //guidance on if to use both or the null/white space
            if (string.IsNullOrEmpty(_userName))
            {
                _logger.LogError("User name is empty");
                return;
            }
            if (string.IsNullOrWhiteSpace(_userName))
            {
                _logger.LogError("User name is empty");
                return;
            }

            try
            {
                //Starts the chat and refreshes UI?
                _isChatting = true;
                await Task.Delay(1);

                //if want to remove prior messages
                //_messages.Clear();

                //Create the chat clinet
                string baseUrl = _navigationManager.BaseUri;
                _hubUrl = baseUrl.TrimEnd('/') + ChatHubRoutes.HubUrl;
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .Build();
                _hubConnection.On<string,string>("Broadcast", BroadcastMessage);
                await _hubConnection.StartAsync();

                await SendAsync($"[Notice] {_userName} has joined the chat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting chat{ ex.Message}");
                _isChatting = false;
            }   
        }
        public void BroadcastMessage(string name, string message)
        {
            bool isMine = name.Equals(_userName, StringComparison.OrdinalIgnoreCase);
            _messages.Add(new ChatMessage(name, message, isMine));

            //InvokeAsync(StateHasChanged);
        }

        public async Task DisconnectAsync()
        {
            if (_isChatting)
            {
                await SendAsync($"[Notice] {_userName} has left the chat");
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                _isChatting = false;
            }
        }

        public async Task SendAsync(string message)
        {
            //adjust based on earlier guidance
            if (_isChatting && !string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    await _hubConnection.SendAsync("Broadcast", _userName, message);
                    _message = string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending message: {ex.Message}");
                }
            }
        }   

        private bool _isChatting = false;
        private string _userName;
        private string _message;

        private List<ChatMessage> _messages = new List<ChatMessage>();
        private string _hubUrl;
        private HubConnection _hubConnection;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly NavigationManager _navigationManager;
    }
}
