using ActiproSoftware.Windows.Extensions;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace dymaptic.Chat.ArcGIS;

internal class DymapticChatDockpaneViewModel : DockPane
{
    #region Private Properties
    private const string DockPaneId = "DockpaneChat_DympaticChatDockpane";

    private readonly ObservableCollection<ArcGISMessage> _messages = new ObservableCollection<ArcGISMessage>();
    private readonly object _lockMessageCollections = new object();
    private readonly ReadOnlyObservableCollection<ArcGISMessage> _readOnlyListOfMessages;

    private ICommand _sendMessageCommand;
    private ICommand _clearMessagesCommand;
    private ICommand _copyMessageCommand;
    private ICommand _insertMessageCommand;

    private string? _userName;
    private string? _organizationId;
    private ArcGISPortal? _portal;
    private ChatManager? _chatManager;

    private bool _onStartup = true;
    private HttpClient _errorClient;

    public string ChatIconUrl = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png";

    #endregion

    #region Public Properties

    public ReadOnlyObservableCollection<ArcGISMessage> Messages => _readOnlyListOfMessages;

    public string MessageText { get; set; }

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

    public ICommand InsertMessageCommand => _insertMessageCommand;

    #endregion

    #region Constructor

    protected DymapticChatDockpaneViewModel()
    {
        // setup the lists and sync between background and UI
        _readOnlyListOfMessages = new ReadOnlyObservableCollection<ArcGISMessage>(_messages);
        BindingOperations.EnableCollectionSynchronization(_readOnlyListOfMessages, _lockMessageCollections);

        // set up the command to retrieve the maps
        _sendMessageCommand = new RelayCommand(SendMessage, () => !string.IsNullOrEmpty(MessageText));

        _clearMessagesCommand = new RelayCommand(ClearMessages, true);

        _copyMessageCommand = new RelayCommand(CopyMessageToClipboard, (m) => true);
        _insertMessageCommand = new RelayCommand(CreateArcadePopupAsync, (m) => true);

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
                    var portalinfo = portal.GetPortalInfoAsync().Result;
                    _organizationId = portalinfo.OrganizationId;
                    _portal = portal;
                }
                else
                {
                    _userName = "User";
                }
            }
            _chatManager = new ChatManager(_portal, ChatIconUrl);
            _chatManager.ConnectionSuccess += OnConnectionSuccess;
            _chatManager.ConnectionError += OnConnectionError;
        });

        _ = Utils.RunOnUIThread(() =>
        {
            var waitingMessage = _messages.LastOrDefault();
            if (waitingMessage?.Type == MessageType.Waiting)
            {
                _messages.Remove(waitingMessage);
            }
            _messages.Add(_welcomeMessage);
        });

        MessageText = string.Empty;

        //layers combobox
        ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
        if (MapView.Active != null)
        {
            OnActiveMapViewChanged(new ActiveMapViewChangedEventArgs(MapView.Active, null));
        }
        LayersAddedEvent.Subscribe(OnLayersAdd);
        LayersRemovedEvent.Subscribe(OnLayersRem);
    }

    private HttpClient GetErrorHttpClient()
    {
        //todo: check if the cookies have been updated, or if they are null
        if (_errorClient == null)
        {

            var handler = new HttpClientHandler();
            handler.CookieContainer = _chatManager?.GetCookieContainer()!;

            _errorClient = new HttpClient(handler);

        }

        return _errorClient;
    }

    private async void OnConnectionError(object? sender, ChatEventArgs e)
    {
        await Utils.RunOnUIThread(() =>
        {
            var waitingMessage = _messages.LastOrDefault();
            if (waitingMessage?.Type == MessageType.Waiting)
            {
                _messages.Remove(waitingMessage);
            }
            _messages.Add(e.Message!);
        });
    }

    private void OnConnectionSuccess(object? sender, EventArgs e)
    {
        //if there was an error messages, we remove it
        var waitingMessage = _messages.LastOrDefault();
        if (waitingMessage?.Type == MessageType.Waiting)
        {
            _messages.Remove(waitingMessage);
        }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// OnShow override to subscribe to the event when the dockpane is made visible.
    /// This will start or stop the hub connection when the dockpane is shown or hidden.
    /// It can get called multiple times on application initalization and when the dockpane is shown or hidden.
    /// </summary>
    /// <param name="isVisible"></param>
    protected override void OnShow(bool isVisible)
    {
        Debug.WriteLine("Called OnShow");

        if (isVisible)
        {
            //ignore the first time this is "hidden" on startup
            _onStartup = false;

            Debug.WriteLine("SignalR Connecting");
            //there is a possible race condition where the dockpane is shown before the ArcGIS portal is created
            //it would be good to include something to start the hub if the portal is created after the window is open
            if (_chatManager != null)
            {
                _ = Task.Run(() => _chatManager.StartHubConnection());
            }
        }
        //OnShow gets called on first load, so we need to make sure we don't unsubscribe on first load.
        //ArcGIS api seems to hit this multiple times on load when the window is open.
        else if (!isVisible && !_onStartup) //Unsubscribe as the dockpane closes.
        {
            if (_chatManager != null)
            {
                _ = Task.Run(() => _chatManager!.StopHubConnection());
            }
        }
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


    public string Heading
    {
        get { return _heading; }
        set
        {
            SetProperty(ref _heading, value, () => Heading);
        }
    }

    #endregion Show dockpane 


    #region Private Helpers
    private CancellationTokenSource _sendCancellationTokenSource = new CancellationTokenSource();


    /// <summary>
    /// Method for sending chat questions to the AI.
    /// </summary>
    private async void SendMessage()
    {
        Debug.WriteLine("SendMessage");

        //cancel previous message, create a new token and save it locally to track if the next message cancels it
        _sendCancellationTokenSource.Cancel();
        //we want to empty the message builder if we are interrupting a previous message
        _responseMessageBuilder.Clear();
        _sendCancellationTokenSource = new CancellationTokenSource();
        var sendCancellationTokenSource = _sendCancellationTokenSource;

        if (_chatManager != null && _chatManager.IsConnected())
        {
            var message = new ArcGISMessage(MessageText, DyChatSenderType.User, _userName)
            {
                LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                ShortName = _userName?.FirstOrDefault().ToString() ?? "",
                Type = MessageType.Message
            };

            //we create a temp message while we wait for actual server response
            var waitingMessage = new ArcGISMessage("thinking...", DyChatSenderType.Bot, "dymaptic")
            {
                LocalTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                Icon = ChatIconUrl,
                Type = MessageType.Waiting
            };

            await BuildMessageSettings();
            var messageSettings = Module1.GetMessageSettings();
            //populate the Selected layer from a application selected layer if there is one and the curretly selected layer is empty
            if (string.IsNullOrEmpty(messageSettings?.DyChatContext?.CurrentLayer))
            {
                var layer = MapView.Active.GetSelectedLayers().OfType<FeatureLayer>().FirstOrDefault(); ;
                if (layer != null)
                {
                    SelectedFeatureLayer = layer;
                }
                else
                {
                    MessageBox.Show(
                        "Please select a layer in the dropdown of the chat window to add this expression to",
                        "Creation Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

            }

            // _messages.add needs to be on the MCT
            await QueuedTask.Run(async () =>
            {
                _messages.Add(message);
                MessageText = "";
                NotifyPropertyChanged(() => MessageText);
                _messages.Add(waitingMessage);

                ArcGISMessage responseMessage = new ArcGISMessage(string.Empty, DyChatSenderType.Bot, "dymaptic")
                {
                    Icon = ChatIconUrl,
                    Type = MessageType.Message
                };

                try
                {

                    await foreach (char c in _chatManager.QueryChatServer(
                                       new DyRequest(_messages.Cast<DyChatMessage>().ToList(), messageSettings?.DyChatContext ?? null,
                                           new DyUserInfo(_userName, _organizationId, _portal?.PortalUri.AbsoluteUri, _portal?.GetToken())), sendCancellationTokenSource.Token))
                    {
                        if (_messages.Last().Type == MessageType.Waiting)
                        {
                            _messages.Remove(_messages.Last());
                        }

                        if (!Messages.Contains(responseMessage))
                        {
                            _messages.Add(responseMessage);
                        }
                        _responseMessageBuilder.Append(c);
                        responseMessage.DisplayContent = _responseMessageBuilder.ToString();
                    }

                    _responseMessageBuilder.Clear();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex.Message);

                    //TODO: use app.config
                    //make this a POST action on the server
                    //add parameters including the exception and a message stating where this happened
                    //handle if the server is down, or some other error happens and swallow it
                    GetErrorHttpClient()
                        .PostAsync(
                            "https://dy-chat.azurewebsites.net/LogError?ErrorToken=AC72107E-9536-4E20-A1B8-B299669399B6&ErrorMessage=asdasdasddsasasd",
                            null);
#else
#endif
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
            _messages.Add(_welcomeMessage);
        });
    }


    private void CopyMessageToClipboard(object messageObject)
    {
        try
        {
            // Copy text to clipboard
            Clipboard.SetText(messageObject.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    private StringBuilder _responseMessageBuilder = new();
    private ArcGISMessage _welcomeMessage => new ArcGISMessage(
        "Hello! Welcome to dymaptic chat! \r\n Start typing a question and let's make some awesome maps. \r\n I am powered by AI, so please verify any suggestions I make.",
        DyChatSenderType.Bot, "dymaptic")
    {
        LocalTime = DateTime.Now.ToString(CultureInfo.InvariantCulture),
        Icon = ChatIconUrl,
        Type = MessageType.Message
    };

    #endregion Private Helpers

    #region dropdown

    public ObservableCollection<FeatureLayer> FeatureLayers { get; set; } = new ObservableCollection<FeatureLayer>();

    public Dictionary<Layer, BitmapSource> FeatureLayerIcons { get; set; } = new Dictionary<Layer, BitmapSource>();

    /// <summary>
    /// Refactored the settings to get the whole layer object instead of just the name.  This allows the name to be used for context and the layer object to be "worked on" rather than just the name.
    /// </summary>
    public Layer? SelectedFeatureLayer
    {
        get
        {
            return _selectedLayer;
        }
        set
        {
            if (_selectedLayer != value)
            {
                _selectedLayer = FeatureLayers?.FirstOrDefault(x =>
                     x.Name.Equals(value?.Name, StringComparison.InvariantCultureIgnoreCase));

                _messageSettings.DyChatContext!.CurrentLayer = value?.Name;
                Module1.SaveMessageSettings(_messageSettings);
                NotifyPropertyChanged(() => SelectedFeatureLayer);
            }
        }
    }

    private Layer? _selectedLayer;

    private MessageSettings _messageSettings
    {
        get
        {
            return Module1.GetMessageSettings();
        }
    }

    /// <summary>
    /// Tracks when layers are added to the table of contents and then reflects that in the combobox values
    /// </summary>
    private async void OnLayersAdd(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersadded event (which runs on background)
        var existingLayerNames = FeatureLayers.Select(i => i.ToString()).ToList();
        foreach (var addedLayer in args.Layers)
        {
            if (addedLayer is not FeatureLayer featureLayer) continue;
            if (!existingLayerNames.Contains(addedLayer.Name))
            {
                await QueuedTask.Run(() =>
                {
                    // MakeComboBoxItem(addedLayer.GetDefinition() as CIMFeatureLayer);
                    FeatureLayers.Add(featureLayer);

                });
            }
        }
    }


    /// <summary>
    /// Tracks when layers are removed to the table of contents and then reflects that in the combobox values
    /// </summary>
    private void OnLayersRem(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersAdded event (which runs on background)
        var existingLayerNames = FeatureLayers.Select(i => i.ToString()).ToList();
        foreach (var removedLayer in args.Layers)
        {
            if (existingLayerNames.Contains(removedLayer.Name))
            {
                var layer = FeatureLayers.FirstOrDefault(x => x.Name.Equals(removedLayer.Name, StringComparison.InvariantCultureIgnoreCase));
                if (layer != null)
                {
                    _ = Application.Current.Dispatcher.BeginInvoke((Action)(() => { FeatureLayers.Remove(layer); }));
                }
            }

            OnActiveMapViewChanged(new ActiveMapViewChangedEventArgs(MapView.Active, null));
        }
    }

    private async void CreateArcadePopupAsync(object messageObject)
    {

        var selectionLayer = MapView.Active?.Map.GetLayersAsFlattenedList().FirstOrDefault(x =>
            x.Name.Equals(_messageSettings?.DyChatContext?.CurrentLayer, StringComparison.InvariantCultureIgnoreCase));

        if (MapView.Active == null || selectionLayer == null)
        {
            MessageBox.Show("Please select a layer in the dropdown of the chat window to add this expression to",
                "Creation Error", MessageBoxButton.OK, MessageBoxImage.Information);

            return;
        }

        var dockPane = ProApp.DockPaneManager.Find("esri_mapping_popupsDockPane");

        //this is to make sure the popups are populated correctly after we add the new one.
        dockPane.Activate();

        if (dockPane.IsVisible)
            dockPane.Hide();
        //select the layer after we open the window. This is keep it from loading while we add the expression
        MapView.Active.SelectLayers(new[] { selectionLayer });

        await QueuedTask.Run(() =>
        {
            var layerDef = selectionLayer.GetDefinition();
            var popupInfo = layerDef.PopupInfo;

            if (popupInfo == null)
            {
                popupInfo = new CIMPopupInfo()
                {
                    Title = "{OBJECTID}"
                };
                layerDef.PopupInfo = popupInfo;
            }

            var mediaExpression = new CIMExpressionMediaInfo()
            {
                Expression = new CIMExpressionInfo()
                {
                    Expression = messageObject.ToString(),
                    Title = "Custom",
                    ReturnType = ExpressionReturnType.Default
                },
                Column = 0,
                ColumnSpan = 1,
                Row = 0,
                RowSpan = 1,
            };

            var mediaInfos = popupInfo.MediaInfos == null ? new List<CIMMediaInfo>() : popupInfo.MediaInfos.ToList();

            mediaExpression.Column = mediaInfos.Count == 0 ? 0 : mediaInfos.Max((x => x.Column)) + 1;
            mediaExpression.Row = mediaInfos.Count == 0 ? 0 : mediaInfos.Max((x => x.Row)) + 1;
            mediaInfos.Add(mediaExpression);
            popupInfo.MediaInfos = mediaInfos.ToArray();
            selectionLayer.SetDefinition(layerDef);
        });

        //focus on the popup window
        dockPane.Activate();
    }

    public async Task<MessageSettings> BuildMessageSettings()
    {
        // instantiate objects and gets the value of the selected layer from the combobox 'SelectedFeatureLayer'
        List<DyLayer> layerList = new List<DyLayer>();
        List<DyField> layerFieldCollection = new List<DyField>();

        // Get the features that intersect the sketch geometry.
        await QueuedTask.Run(() =>
        {
            foreach (var viewLayer in FeatureLayers!)
            {
                var layerFields = viewLayer.GetFieldDescriptions();
                foreach (var field in layerFields)
                {
                    DyField dyField = new DyField(field.Name, field.Alias, field.Type.ToString());
                    layerFieldCollection.Add(dyField);
                }
                DyLayer dyLayer = new DyLayer(viewLayer.Name, layerFieldCollection);
                layerList.Add(dyLayer);
            }

            // build and return the dyChatContext object to send to settings
            DyChatContext dyChatContext = new DyChatContext(layerList, SelectedFeatureLayer?.Name!);
            _messageSettings.DyChatContext = dyChatContext;
            _messageSettings.DyChatContext.CurrentLayer = SelectedFeatureLayer?.Name;
            Module1.SaveMessageSettings(_messageSettings);

        });

        return _messageSettings;
    }

    /// <summary>
    /// Tracks changes in the active mapview as the source for other adjustments that may occur for example
    /// Layers added/removed in the table of contents.
    /// </summary>
    private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
    {
        FeatureLayers.Clear();
        FeatureLayerIcons.Clear();
        if (args.IncomingView != null)
        {
            //TODO: can other layer types have popups too? should this be a Layer type, rather then a feature Layer?
            //The main issue is layers do not have GetFieldDescriptions, but there may be something else we can do
            var layerlist = args.IncomingView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();

            //Add feature layer names to the combobox
            QueuedTask.Run(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    FeatureLayers.AddRange(layerlist);

                    //this will attempt to re-select the layer that was previously selected when the project was last open
                    if (!string.IsNullOrEmpty(_messageSettings?.DyChatContext?.CurrentLayer))
                    {
                        SelectedFeatureLayer = layerlist?.FirstOrDefault(x =>
                            x.Name.Equals(_messageSettings?.DyChatContext?.CurrentLayer, StringComparison.InvariantCultureIgnoreCase));
                    }
                });

                layerlist.ForEach(x =>
                {
                    var cimFeatureLayer = x.GetDefinition() as CIMFeatureLayer;
                    if (cimFeatureLayer?.Renderer is CIMSimpleRenderer cimRenderer)
                    {
                        var si = new SymbolStyleItem()
                        {
                            Symbol = cimRenderer.Symbol.Symbol,
                            PatchHeight = 16,
                            PatchWidth = 16
                        };
                        FeatureLayerIcons.Add(x, si.PreviewImage as BitmapSource);
                    }
                });


            });
        }
    }

    #endregion dropdown
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

/// <summary>
/// Due to inheritance restrictions, we have to implement INotifyPropertyChanged,
/// but this allows us to stream the content and have the UI update
/// </summary>
/// <param name="Content"></param>
/// <param name="SenderType"></param>
/// <param name="UserName"></param>
public record ArcGISMessage(string Content, DyChatSenderType SenderType, string? UserName = null)
    : DyChatMessage(Content, SenderType, UserName), INotifyPropertyChanged
{

    public string? DisplayContent
    {
        get => Content;
        set
        {
            Content = value;
            NotifyPropertyChanged();
        }
    }

    public string? ShortName { get; set; }
    public string? LocalTime { get; set; }
    public string? Icon { get; set; }
    public MessageType Type { get; set; }

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    protected virtual void NotifyPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
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


