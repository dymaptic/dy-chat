using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using dymaptic.Chat.Shared.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace dymaptic.Chat.ArcGIS
{
    /// <summary>
    /// Saves and loads the ArcGISSchema (layer info) for reference for the chat application
    ///</summary>
    internal class Module1 : Module
    {

        public event EventHandler? SettingsLoaded;
        public event EventHandler? SettingsUpdated;
        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current =>
            _this ?? (_this = (Module1)FrameworkApplication.FindModule("DockpaneChat_Module"));

        public bool SettingsLoadComplete;

        public static MessageSettings GetMessageSettings()
        {
            // if the catalog layers have changed, then start the process to rebuild the settings
            if (_settings == null)
            {
                _settings = new MessageSettings() { DyChatContext = new DyChatContext(new List<DyLayer>(), null )};

            }

            return _settings;
        }
        public static void SaveMessageSettings(MessageSettings messageSettings)
        {

            _settings = messageSettings;
            Current.SettingsUpdated?.Invoke(Current, EventArgs.Empty);
        }

        private static Module1? _this;
        private static MessageSettings? _settings;

        protected override bool CanUnload()
        {
            return true;
        }

        protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
        {
            var settingsValue = settings.Get("ArcGISSchema.Settings") as string;
            if (settingsValue != null)
            {
                _settings = JsonConvert.DeserializeObject<MessageSettings>(settingsValue) ?? new MessageSettings();

            }
            SettingsLoaded?.Invoke(this, EventArgs.Empty);
            SettingsLoadComplete = true;
            return Task.CompletedTask;
        }

        protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
        {
            settings.Add("ArcGISSchema.Settings", JsonConvert.SerializeObject(_settings));
            return Task.CompletedTask;
        }

    }
}
