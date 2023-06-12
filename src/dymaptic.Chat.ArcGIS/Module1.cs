//Copyright 2019 Esri
using Newtonsoft.Json;
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       https://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using dymaptic.Chat.Shared.Data;
using System.Threading.Tasks;
using System;
using System.Configuration;

namespace dymaptic.Chat.ArcGIS
{   
    /// <summary>
    /// This sample shows how to: 
    /// * Create a Dockpane using DAML
    /// * Use the MVVM Pattern with ArcGIS Pro
    /// * Change the Dockpane title
    /// * Add a ListBox to the Dockpane showing the list of maps
    /// * Add a ListBox to the Dockpane showing bookmarks of a selected map
    /// * Use the Map selection changed event to populate a list of bookmarks with bookmarks for the selected map.
    /// * Use Bookmark list selection change event to zoom map view to bookmark
    /// </summary>
    /// <remarks>
    /// 1. In Visual Studio click the Build menu. Then select Build Solution.
    /// 1. Click Start button to open ArcGIS Pro.
    /// 1. ArcGIS Pro will open. 
    /// 1. Open a project with a map view that has bookmarks and click on the 'Dockpane Lab' tab.
    /// 1. Select a Map view from the list of Map views.
    /// 1. The bookmarks for the selected map should appear in the list of bookmarks.  
    /// 1. Select a bookmark to zoom to a given bookmark.
    /// ![UI](Screenshots/Screen.png)
    /// </remarks>
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current => 
            _this ?? (_this = (Module1)FrameworkApplication.FindModule("DockpaneSimple_Module"));
        //        {
        //            get
        //            {
        //                return _this ?? (_this = (Module1) FrameworkApplication.FindModule("DockpaneSimple_Module"));
        //            }
        //        }
        public bool SettingsLoadComplete;

        public static Settings GetSettings()
        {
            if (_settings == null)
            {
                _settings = new Settings();
            }
            return _settings;
        }
        public static void SaveSettings(Settings settings)
        {
            _settings = settings;
            Current.SettingsUpdated?.Invoke(Current, EventArgs.Empty);
        }


        private static Module1 _this;
        private static Settings _settings;

        protected override bool CanUnload()
        {
            return true;
        }

        protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
        {
            var settingsValue = settings.Get("ArcGISSchema.Settings") as string;
            if (settingsValue != null)
            {
                _settings = JsonConvert.DeserializeObject<Settings>(settingsValue) ?? new Settings();
                
            }
            SettingsLoaded?.Invoke(this, EventArgs.Empty);
            SettingsLoadComplete = true;
            return Task.FromResult(0);
        }

        protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
        {
            settings.Add("ArcGISSchema.Settings", JsonConvert.SerializeObject(_settings));
            return Task.FromResult(0);
        }
    }
}
