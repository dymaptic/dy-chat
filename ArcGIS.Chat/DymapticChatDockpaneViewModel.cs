
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace dymaptic.Chat.ArcGIS
{
    internal class DymapticChatDockpaneViewModel : DockPane
    {
        #region Private Properties
        private const string DockPaneId = "DockpaneSimple_DympaticChatDockpane";

        private readonly ObservableCollection<Message> _messages = new ObservableCollection<Message>();

        private readonly object _lockMessageCollections = new object();

        private readonly ReadOnlyObservableCollection<Message> _readOnlyListOfMessages;

        private Map _selectedMap;

        private ICommand _sendMessageCommand;

        #endregion

        #region Public Properties

        public ReadOnlyObservableCollection<Message> Messages => _readOnlyListOfMessages;

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
        /// Implement a 'RelayCommand' to retrieve all maps from the current project
        /// </summary>
        public ICommand SendMessageCommand => _sendMessageCommand;

        #endregion

        #region CTor

        protected DymapticChatDockpaneViewModel()
        {
            // setup the lists and sync between background and UI
            _readOnlyListOfMessages = new ReadOnlyObservableCollection<Message>(_messages);
            BindingOperations.EnableCollectionSynchronization(_readOnlyListOfMessages, _lockMessageCollections);

            // set up the command to retrieve the maps
            _sendMessageCommand = new RelayCommand(() => SendMessage(), () => !string.IsNullOrEmpty(MessageText));
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


            if (Project.Current != null)
            {
                // add needs to be on the MCT
                await QueuedTask.Run(() =>
                {
                    _messages.Clear();
                    var message = new Message()
                    {
                        User = "Me",
                        Text = MessageText,
                        Time = System.DateTime.Now.ToString()
                    };

                    _messages.Add(message);

                    var message2 = new Message()
                    {
                        User = "dymaptic",
                        Text = "Look Dave, I can see you're really upset about this. I honestly think you ought to sit down calmly, take a stress pill, and think things over.",
                        Time = System.DateTime.Now.ToString(),
                        Icon = "pack://application:,,,/dymaptic.Chat.ArcGIS;component/Images/dymaptic.png"
                    };

                    _messages.Add(message2);
                    MessageText = "";
                    NotifyPropertyChanged(() => MessageText);
                });
            }
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

    public class Message
    {
        public string Text { get; set; }
        public string User { get; set; }
        public string Time { get; set; }
        public string Icon { get; set; }
    }
}
