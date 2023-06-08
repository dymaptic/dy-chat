using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Data;
using System.Windows.Input;
using dymaptic.Chat.Shared.Data;
using System.Windows;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Internal.Mapping;


namespace dymaptic.Chat.ArcGIS;

internal class DymapticChatDockpaneViewModel : DockPane
{
    #region Private Properties
    private const string DockPaneId = "DockpaneSimple_DympaticChatDockpane";

    private readonly ObservableCollection<ArcGISMessage> _messages = new ObservableCollection<ArcGISMessage>();

    private readonly object _lockMessageCollections = new object();

    private readonly ReadOnlyObservableCollection<ArcGISMessage> _readOnlyListOfMessages;

    private ICommand _sendMessageCommand;

    private ICommand _clearMessagesCommand;

    private ICommand _copyMessageCommand;

    private bool _onStartup = true;

    private string _userName { get; set; }
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private string _chatIconURL = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png";

    private Map _selectedMap;

    #endregion

    #region Public Properties

    public ReadOnlyObservableCollection<ArcGISMessage> Messages => _readOnlyListOfMessages;

    public string MessageText { get; set; }


    /// <summary>
    /// This is where we store the selected map 
    /// </summary>
    public Map SelectedMap
    {
        get { return _selectedMap; }
        set
        {
            Debug.WriteLine("selected map");
            // make sure we're on the UI thread
            Utils.RunOnUIThread(() =>
            {
                SetProperty(ref _selectedMap, value, () => SelectedMap);
                if (_selectedMap != null)
                {
                    // open /activate the map
                    Utils.OpenAndActivateMap(_selectedMap.URI);
                }
            });
            Debug.WriteLine("selected map opened and activated map");
            // no need to await
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // UpdateBookmarks(_selectedMap);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Debug.WriteLine("updated bookmarks");
        }
    }

    /// <summary>
    /// Send a message to the chat hub
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// Clear all messages in the chat window
    /// </summary>
    public ICommand ClearMessagesCommand => _clearMessagesCommand;

    /// <summary>
    /// Copies the message to the clipboard
    /// </summary>
    public ICommand CopyMessageCommand => _copyMessageCommand;


    #endregion

    #region CTor

    protected DymapticChatDockpaneViewModel()
    {
        // setup the lists and sync between background and UI
        _readOnlyListOfMessages = new ReadOnlyObservableCollection<ArcGISMessage>(_messages);
        BindingOperations.EnableCollectionSynchronization(_readOnlyListOfMessages, _lockMessageCollections);

        // set up the command to retrieve the maps
        _sendMessageCommand = new RelayCommand(() => SendMessage(), () => !string.IsNullOrEmpty(MessageText));

        _clearMessagesCommand = new RelayCommand(() => ClearMessages(), true);

        _copyMessageCommand = new RelayCommand((message) => CopyMessageToClipboard(message), (m) => true);


        QueuedTask.Run(() =>
        {
            var portal = ArcGISPortalManager.Current.GetActivePortal();
            if (portal != null)
            {
                // make sure is signed in
                var isSignedOn = portal.IsSignedOn();
                if (isSignedOn)
                {
                    _userName = portal.GetSignOnUsername();
                }
                else
                {
                    _userName = "User";
                }
            }
        });
        //TODO: this should be in a config file, and we probably need an actual server to resolve to when we publish this
        var hubUrl = "http://localhost:5145"; //"https://localhost:7048";

        _chatServer = new HubConnectionBuilder()
            .WithUrl(hubUrl + ChatHubRoutes.HubUrl)
            .WithAutomaticReconnect()
            .Build();

        _chatServer.Reconnecting += ChatServer_Reconnecting;

        _chatServer.On<DyChatMessage>(ChatHubRoutes.ResponseMessage, ChatServerResponseHandler);

        //add welcome message
        var welcomeMessage = new ArcGISMessage(
            "Hello! Welcome to dymaptic chat! \r\n Start typing a question and lets make some awesome maps. \r\n I am powered by AI, so please verify any suggestions I make.",
            DyChatSenderType.Bot, "dymaptic")
        {
            LocalTime = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            Icon = _chatIconURL,
            Type = MessageType.Message
        };

        _ = Utils.RunOnUIThread(() =>
        {
            var waitingMessage = _messages.LastOrDefault();
            if (waitingMessage?.Type == MessageType.Waiting)
            {
                _messages.Remove(waitingMessage);
            }
            _messages.Add(welcomeMessage);
        });
    }

    private async Task StartHubConnection()
    {
        var hubCancellationToken = _cancellationTokenSource.Token;
        //we loop here until we can connect to the chat server
        while (true)
        {
            try
            {
                //we stop if the connection is cancelled or if we're already connected
                if (hubCancellationToken.IsCancellationRequested || _chatServer.State == HubConnectionState.Connected)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    return;
                }

                await _chatServer.StartAsync(hubCancellationToken);

                //if there was an error messages, we remove it
                var waitingMessage = _messages.LastOrDefault();
                if (waitingMessage?.Type == MessageType.Waiting)
                {
                    _messages.Remove(waitingMessage);
                }

                return;
            }
            catch (Exception ex)
            {
                //we add a message to the chat window when we can't connect so the user knows what's going on
                Console.WriteLine(ex.Message);

                var errorMessage = new ArcGISMessage("Error: unable to connect to the chat server, attempting to reconnect",
                    DyChatSenderType.Bot, "dymaptic")
                {
                    LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                    Icon = _chatIconURL,
                    Type = MessageType.Waiting
                };

                await Utils.RunOnUIThread(() =>
                {
                    var waitingMessage = _messages.LastOrDefault();
                    if (waitingMessage?.Type == MessageType.Waiting)
                    {
                        _messages.Remove(waitingMessage);
                    }
                    _messages.Add(errorMessage);
                });

                // Failed to connect, trying again in 5000 ms.
                await Task.Delay(5000);

            }
        }
    }

    /// <summary>
    /// This handles the response from the chat server
    /// </summary>
    /// <param name="message"></param>
    private void ChatServerResponseHandler(DyChatMessage message)
    {
        var messageModel = new ArcGISMessage(message.Content, message.SenderType,
            message.Username)
        {
            CopyBody = message.Content,
            LocalTime = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            Icon = _chatIconURL,
            Type = MessageType.Message
        };

        Utils.RunOnUIThread(() =>
        {
            var waitingMessage = _messages.Last();
            if (waitingMessage.Type == MessageType.Waiting)
            {
                _messages.Remove(waitingMessage);
            }
            _messages.Add(messageModel);
        });
    }

    private Task ChatServer_Reconnecting(Exception arg)
    {
        var waitingMessage = new ArcGISMessage("Error: attempting to reconnect to the server",
            DyChatSenderType.Bot, "dymaptic")
        {
            LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
            Icon = _chatIconURL,
            Type = MessageType.Waiting
        };
        Utils.RunOnUIThread(() =>
        {
            var previousMessage = _messages.Last();
            if (previousMessage.Type == MessageType.Waiting)
            {
                _messages.Remove(previousMessage);
            }
            _messages.Add(waitingMessage);
        });
        return Task.CompletedTask;
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Override to implement custom initialization code for this dockpane
    /// </summary>
    /// <returns></returns>
    protected override Task InitializeAsync()
    {
        ProjectItemsChangedEvent.Subscribe(OnProjectCollectionChanged, false);
        return base.InitializeAsync();
    }

    /// <summary>
    /// OnShow override to subscribe to the event when the dockpane is made visible.
    /// This will start or stop the hub connection when the dockpane is shown or hidden.
    /// </summary>
    /// <param name="isVisible"></param>
    protected override void OnShow(bool isVisible)
    {
        Debug.WriteLine("Called OnShow");

        if (isVisible)
        {
            Debug.WriteLine("SignalR Connecting");
            _ = StartHubConnection();
            _onStartup = false;
            if (_disconnectTimer is { Enabled: true })
            {
                _disconnectTimer.Stop();
                _disconnectTimer.Dispose();
                _disconnectTimer = null;
            }
        }
        //OnShow gets called on first load, so we need to make sure we don't unsubscribe on first load.
        //ArcGIS api seems to hit this multiple times on load when the window is open.
        //make a timer and let that shut off the connection after a few minutes.
        //and then cancel the timer if the window is set to visible again.
        else if (!isVisible && !_onStartup) //Unsubscribe as the dockpane closes.
        {
            if (_disconnectTimer == null)
            {
               _disconnectTimer = new System.Timers.Timer(180000);// 3min
                _disconnectTimer.Elapsed += new ElapsedEventHandler(OnDisconnectEvent);
                _disconnectTimer.Start();
                Debug.WriteLine("SignalR disconnecting");
            }

        }
    }

    private System.Timers.Timer _disconnectTimer;//= new System.Timers.Timer();

    private void OnDisconnectEvent(object sender, ElapsedEventArgs e)
    {
        Debug.WriteLine("SignalR disconnected");
        if (_chatServer.State == HubConnectionState.Connected)
        {
            _ = _chatServer.StopAsync();
        }
        else if (_chatServer.State != HubConnectionState.Disconnected)
        {
            _cancellationTokenSource.Cancel();
        }
        _disconnectTimer.Stop();
        _disconnectTimer.Dispose();
        _disconnectTimer = null;
    }

    #endregion

    #region Show dockpane 
    /// <summary>
    /// Show the DockPane.
    /// </summary>
    internal static void Show()
    {
        var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
        pane?.Activate();
    }

    /// <summary>
    /// Text shown near the top of the DockPane.
    /// </summary>
    private string _heading = "dympatic Chat";

    private readonly HubConnection _chatServer;


    public string Heading
    {
        get { return _heading; }
        set
        {
            SetProperty(ref _heading, value, () => Heading);
        }
    }

    #endregion Show dockpane 

    #region Subscribed Events

    /// <summary>
    /// Subscribe to Project Items Changed events which is getting called each
    /// time the project items change which happens when a new map is added or removed in ArcGIS Pro
    /// </summary>
    /// <param name="args">ProjectItemsChangedEventArgs</param>
    private void OnProjectCollectionChanged(ProjectItemsChangedEventArgs args)
    {
        if (args == null)
            return;
        var mapItem = args.ProjectItem as MapProjectItem;
        if (mapItem == null)
            return;

        // new project item was added
        //switch (args.Action)
        //{
        //    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
        //        {
        //            var foundItem = _listOfMaps.FirstOrDefault(m => m.URI == mapItem.Path);
        //            // one cannot be found; so add it to our list
        //            if (foundItem == null)
        //            {
        //                _listOfMaps.Add(mapItem.GetMap());
        //            }
        //        }
        //        break;
        //    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
        //        {
        //            Map map = mapItem.GetMap();
        //            // if this is the selected map, resest
        //            if (SelectedMap == map)
        //                SelectedMap = null;

        //            // remove from the collection
        //            if (_listOfMaps.Contains(map))
        //            {
        //                _listOfMaps.Remove(map);
        //            }
        //        }
        //        break;
        //}
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Method for retrieving map items in the project.
    /// </summary>
    private async void SendMessage()
    {
        Debug.WriteLine("SendMessage");

        if (_chatServer.State == HubConnectionState.Connected)
        {
            var message = new ArcGISMessage(MessageText, DyChatSenderType.User, _userName)
            {
                LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                ShortName = _userName[0].ToString(),
                Type = MessageType.Message
            };

            //we create a temp message while we wait for actual server response
            var waitingMessage = new ArcGISMessage("thinking...", DyChatSenderType.Bot, "dymaptic")
            {
                LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                Icon = _chatIconURL,
                Type = MessageType.Waiting
            };

            // _messages.add needs to be on the MCT
            await QueuedTask.Run(async () =>
            {
                _messages.Add(message);
                MessageText = "";
                NotifyPropertyChanged(() => MessageText);
                _messages.Add(waitingMessage);

                try
                {
                    await foreach (char c in _chatServer.StreamAsync<char>(ChatHubRoutes.QueryChatService,
                                       new DyRequest(_messages.Cast<DyChatMessage>().ToList(), _chatContext)))
                    {
                        _responseMessageBuilder.Append(c);
                        _messages.RemoveAt(-1);
                        _messages.Add(new ArcGISMessage(_responseMessageBuilder.ToString(), DyChatSenderType.Bot, "dymaptic")
                        {
                            LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                            Icon = _chatIconURL,
                            Type = MessageType.Message
                        });
                    }
                    
                    _responseMessageBuilder.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }
    }

    private async void ClearMessages()
    {
        // add needs to be on the MCT
        await QueuedTask.Run(() =>
        {
            _messages.Clear();
        });
    }


    private void CopyMessageToClipboard(object messageObject)
    {
        if (messageObject is ArcGISMessage message)
        {
            // Copy text to clipboard
            Clipboard.SetText(message.CopyBody);
        }
    }

    private DyLayer treesLayer = new DyLayer("Special_Tree_Layer", new List<DyField>() {new DyField("Tree_Name", "Tree Name", "string"), new DyField("TT", "Type", "string")});
    private DyLayer parcelLayer = new DyLayer("My_Parcels", new List<DyField>() {new DyField("Parcel_Name", "Parcel Name", "string")});
    private DyChatContext _chatContext => new(new List<DyLayer>()
    {
        treesLayer, parcelLayer
    }, "My_Parcels");

    private StringBuilder _responseMessageBuilder = new();

    #endregion Private Helpers
}

/// <summary>
/// Button implementation to show the DockPane.
/// </summary>
internal class DymapticChatDockpane_ShowButton : Button
{
    protected override void OnClick()
    {
        DymapticChatDockpaneViewModel.Show();
    }
}

public record ArcGISMessage(string Content, DyChatSenderType SenderType, string? UserName = null) 
    : DyChatMessage(Content, SenderType, UserName)
{
    public string ShortName { get; set; }
    public string LocalTime { get; set; }
    public string Icon { get; set; }
    public MessageType Type { get; set; }
    public string CopyBody { get; set; }
}

/// <summary>
/// waiting messages are designed to be updated/removed based on the status of the server.
/// "Message" messages are user prompts, or actual responses from the server.
/// </summary>
public enum MessageType
{
    Waiting,
    Message
}
