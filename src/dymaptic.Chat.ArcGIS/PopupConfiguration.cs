
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Internal.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
using ArcGIS.Desktop.Internal.Core.Behaviors;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Internal.Mapping.Popups;
using ArcGIS.Desktop.Internal.Framework.Behaviors;
using ArcGIS.Desktop.Internal.Mapping.PropertyPages;
using ArcGIS.Desktop.Internal.Mapping.Controls.TextExpressionBuilder;
using ArcGIS.Core.CIM;
using System.Collections.ObjectModel;
using ArcGIS.Desktop.Internal.Core;

namespace dymaptic.Chat.ArcGIS;

public class PopupConfiguration : MapTool
{
    public PopupConfiguration()
    {
        OpenPopupConfiguration();
    }

    private async Task OpenPopupConfiguration()
    {
        //replicate get selected feature, input right click action, select configure popup action, new popup instance
        var mv = MapView.Active;
        var selection = _settings.DyChatContext.CurrentLayer;
        if (mv == null || selection == null) return;
        
        Layer? selectionLayer = _allCurrentLayers?.FirstOrDefault(l => l.Name == selection);
        mv.SelectLayers(new[] { selectionLayer });

        // This is the primary Working call to open the popup dock pane ...but doesnt create a new expression item-Opens configure popup pane.
        ProApp.DockPaneManager.Find("esri_mapping_popupsDockPane").Activate();

    }

    List<FeatureLayer>? _allCurrentLayers = MapView.Active?.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private Settings _settings = Module1.GetSettings();
    
}
