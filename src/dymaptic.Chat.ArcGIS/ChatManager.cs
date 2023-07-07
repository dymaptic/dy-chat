using ArcGIS.Desktop.Core;
using dymaptic.Chat.Shared.Data;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// encapsulates the chat server connection and message sending to allow for easier connect/disconnect
/// </summary>
public class ChatManager
{
    public ChatManager(ArcGISPortal? portal, string chatIconUrl)
    {
        _portal = portal;
        _chatIconUrl = chatIconUrl;
    }
    public event EventHandler<ChatEventArgs>? ConnectionError;
    public event EventHandler? ConnectionSuccess;

    public CookieContainer? GetCookieContainer()
    {
        return _cookies;
    }
    public async Task StartHubConnection()
    {
        if (_disconnectTimer is { Enabled: true })
        {
            _disconnectTimer.Stop();
            _disconnectTimer.Dispose();
            _disconnectTimer = null;
        }

        var hubCancellationToken = _cancellationTokenSource.Token;
        //we loop here until we can connect to the chat server
        if (!_starting)
        {
            while (true)
            {
                _starting = true;
                try
                {
                    if (_chatServer == null)
                    {
                        //other urls are for testing

                        //TODO: move to the app config
                        var hubUrl = "https://dy-chat.azurewebsites.net"; //"http://localhost:5145"; //"https://localhost:7048";

                        //login, to get cookies
                        //then set cookies in the hub connection
                        //the cookie container captures the cookies from a http session and re-uses them.
                        //this allows us to authenticate with the server and use the cookies on the hub connection
                        _cookies = new CookieContainer();
                        var handler = new HttpClientHandler();
                        handler.CookieContainer = _cookies;

                        var client = new HttpClient(handler);
                        var result = await client.GetAsync(hubUrl + "/arcgispro-login?token=" + _portal?.GetToken(),
                            hubCancellationToken);
                        if (result.IsSuccessStatusCode)
                        {
                            _chatServer = new HubConnectionBuilder()
                                .WithUrl(hubUrl + ChatHubRoutes.HubUrl, (c) => { c.Cookies = _cookies; })
                                .WithAutomaticReconnect()
                                .Build();

                            _chatServer.Reconnecting += ChatServer_Reconnecting!;
                            _chatServer.Reconnected += ChatServer_Reconnected;
                        }
                        else
                        {
                            FailAuthentication();
                            return;
                        }
                    }

                    //we stop if the connection is cancelled or if we're already connected
                    if (hubCancellationToken.IsCancellationRequested ||
                        _chatServer.State == HubConnectionState.Connected)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                    }
                    else
                    {
                        await _chatServer.StartAsync(hubCancellationToken);
                    }

                    ConnectionSuccess?.Invoke(this, EventArgs.Empty);
                    _starting = false;
                    return;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    FailAuthentication();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //we add a message to the chat window when we can't connect so the user knows what's going on

                    ConnectionError?.Invoke(this, new ChatEventArgs() { Message = _errorMessage });
                    if (_chatServer != null)
                    {
                        _chatServer.Reconnecting -= ChatServer_Reconnecting;
                        _chatServer.Reconnected -= ChatServer_Reconnected;
                    }

                    _chatServer = null;

                    // Failed to connect, trying again in 5000 ms.
                    await Task.Delay(5000);
                }
            }
        }
    }

    private void FailAuthentication()
    {
        //this happens when they are not authorized to connect to the chat server

        var errorMessage = new ArcGISMessage(
            SystemMessages.Forbidden,
            DyChatSenderType.Bot, "dymaptic")
        {
            LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
            Icon = _chatIconUrl,
            Type = MessageType.Waiting
        };

        ConnectionError?.Invoke(this, new ChatEventArgs() { Message = errorMessage });

        if (_chatServer != null)
        {
            _chatServer.Reconnecting -= ChatServer_Reconnecting;
            _chatServer.Reconnected -= ChatServer_Reconnected;
        }

        _chatServer = null;
        _starting = false;
    }


    //when the connection is requested to stop, we start a timer to disconnect in the event they closed the window accidentally
    public void StopHubConnection()
    {
        if (_disconnectTimer == null)
        {
            _disconnectTimer = new System.Timers.Timer(180000); // 3min 180000
            _disconnectTimer.Elapsed += OnDisconnectEvent!;
            _disconnectTimer.Start();
            Debug.WriteLine("SignalR disconnecting");
        }
    }


    public bool IsConnected()
    {
        return _chatServer?.State == HubConnectionState.Connected;
    }

    public IAsyncEnumerable<char> QueryChatServer(DyRequest dyRequest, CancellationToken token)
    {
        return _chatServer?.StreamAsync<char>(ChatHubRoutes.QueryChatService, dyRequest, token)!;
    }


    private Task ChatServer_Reconnected(string? arg)
    {
        ConnectionSuccess?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }


    private Task ChatServer_Reconnecting(Exception? arg)
    {
        var waitingMessage = new ArcGISMessage("Error: attempting to reconnect to the server",
            DyChatSenderType.Bot, "dymaptic")
        {
            LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
            Icon = _chatIconUrl,
            Type = MessageType.Waiting
        };

        ConnectionError?.Invoke(this, new ChatEventArgs() { Message = waitingMessage });
        return Task.CompletedTask;
    }

    private void OnDisconnectEvent(object sender, ElapsedEventArgs e)
    {
        Debug.WriteLine("SignalR disconnected");
        if (_chatServer != null)
        {
            if (_chatServer.State == HubConnectionState.Connected)
            {
                _ = _chatServer.StopAsync();
            }
            else if (_chatServer.State != HubConnectionState.Disconnected)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        _disconnectTimer!.Stop();
        _disconnectTimer.Dispose();
        _disconnectTimer = null;
    }

    private bool _starting = false;

    private System.Timers.Timer? _disconnectTimer = null;

    private HubConnection? _chatServer;

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly string _chatIconUrl;

    private readonly ArcGISPortal? _portal;

    private CookieContainer? _cookies;

    private ArcGISMessage _errorMessage => new ArcGISMessage(
        SystemMessages.Error,
        DyChatSenderType.Bot, "dymaptic")
    {
        LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
        Icon = _chatIconUrl,
        Type = MessageType.Waiting
    };

}

public class ChatEventArgs : EventArgs
{
    public ArcGISMessage? Message { get; set; }
}
