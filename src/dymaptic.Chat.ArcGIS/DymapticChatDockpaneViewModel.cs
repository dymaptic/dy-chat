
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using dymaptic.Chat.Shared.Data;

namespace dymaptic.Chat.ArcGIS;

internal class DymapticChatDockpaneViewModel : DockPane
{
    #region Private Properties
    private const string DockPaneId = "DockpaneSimple_DympaticChatDockpane";

    private readonly ObservableCollection<ArcGISMessage> _messages = new ObservableCollection<ArcGISMessage>();

    private readonly object _lockMessageCollections = new object();

    private readonly ReadOnlyObservableCollection<ArcGISMessage> _readOnlyListOfMessages;

    private Map _selectedMap;

    private ICommand _sendMessageCommand;

    private ICommand _clearMessagesCommand;

    private string _userName { get; set; }

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
            System.Diagnostics.Debug.WriteLine("selected map");
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
            System.Diagnostics.Debug.WriteLine("selected map opened and activated map");
            // no need to await
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // UpdateBookmarks(_selectedMap);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            System.Diagnostics.Debug.WriteLine("updated bookmarks");
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

        var hubUrl = "http://localhost:5145"; //"https://localhost:7048";

        _chatServer = new HubConnectionBuilder()
            .WithUrl(hubUrl + ChatHubRoutes.HubUrl)
            .WithAutomaticReconnect()
            .Build();

        _chatServer.Closed += ChatServer_Closed;
        _chatServer.Reconnecting += ChatServer_Reconnecting;

        _chatServer.On<ChatMessage>(ChatHubRoutes.ResponseMessage, ChatServerResponseHandler);

        try
        {
            _chatServer.StartAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            var errorMessage = new ArcGISMessage()
            {
                Username = "dymaptic",
                Body = "Error: unable to connect to the chat server, attempting to reconnect",
                Time = System.DateTime.Now.ToString(),
                Icon = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png",
                Type = MessageType.Waiting
            };

            QueuedTask.Run(() =>
            {
                var waitingMessage = _messages.Last();
                if (waitingMessage.Type == MessageType.Waiting)
                {
                    _messages.Remove(waitingMessage);
                }
                _messages.Add(errorMessage);
            });
        }
    }

    private void ChatServerResponseHandler(ChatMessage message)
    {
        //TODO REMOVE
       Thread.Sleep(3000);
        var messageModel = new ArcGISMessage()
        {
            Username = message.Username,
            Body = message.Body,
            Time = System.DateTime.Now.ToString(),
            Icon = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png",
            Type = MessageType.Message
        };

        QueuedTask.Run(() =>
        {
            var waitingMessage = _messages.Last();
            if (waitingMessage.Type == MessageType.Waiting)
            {
                _messages.Remove(waitingMessage);
            }
            _messages.Add(messageModel);
        });
    }

    private async Task ChatServer_Closed(Exception error)
    {
        await Task.Delay(new Random().Next(0, 5) * 1000);
        await _chatServer.StartAsync();
    }

    private Task ChatServer_Reconnecting(Exception arg)
    {
        var waitingMessage = new ArcGISMessage()
        {
            Username = "dymaptic",
            Body = "Error: attempting to reconnect to the server",
            Time = System.DateTime.Now.ToString(),
            Icon = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png",
            Type = MessageType.Waiting
        };
        QueuedTask.Run(() =>
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
        System.Diagnostics.Debug.WriteLine("SendMessage");

        if (_chatServer.State == HubConnectionState.Connected)
        {
            var message = new ArcGISMessage()
            {
                Username = _userName,
                Body = MessageText,
                Time = System.DateTime.Now.ToString(),
                ShortName = _userName[0].ToString(),
                Type = MessageType.Message
            };

            var waitingMessage = new ArcGISMessage()
            {
                Username = "dymaptic",
                Body = "thinking....",
                Time = System.DateTime.Now.ToString(),
                Icon = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png",
                Type = MessageType.Waiting
            };
            // add needs to be on the MCT
            await QueuedTask.Run(() =>
            {
                _messages.Add(message);
                MessageText = "";
                NotifyPropertyChanged(() => MessageText);
                _messages.Add(waitingMessage);
            });

            try
            {
                var chatMessage = new ChatMessage(_userName, message.Body, true);

                //is this where we use ConfigureAwait(false)?
                _ = _chatServer.InvokeAsync(ChatHubRoutes.SendMessage,
                    chatMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
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

public class ArcGISMessage : ChatMessage
{
    public string ShortName { get; set; }
    public string Time { get; set; }
    public string Icon { get; set; }
    public MessageType Type { get; set; }
}

public enum MessageType
{
    Waiting,
    Message
}
