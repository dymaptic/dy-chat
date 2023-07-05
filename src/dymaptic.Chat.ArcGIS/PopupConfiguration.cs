
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
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ActiproSoftware.Windows.Extensions;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using System.Windows.Controls.Primitives;

namespace dymaptic.Chat.ArcGIS;

public class PopupConfiguration : MapTool
{
    public PopupConfiguration()
    {
        OpenPopupConfiguration();
        //ProApp.DockPaneManager.Find("_newExpressionsButton").Activate();
    }

    private async Task OpenPopupConfiguration()
    {
        //replicate get selected feature, input right click action, select configure popup action, new popup instance
        var mv = MapView.Active;
        if (_settings == null)
        {
            _settings = Module1.GetMessageSettings();
        }
        //var selection = _settings?.DyChatContext.SelectedFeatureLayer.Name;
        var selection2 = _settings?.SelectedFeatureLayer.Name;
        //if (mv == null || selection == null) return;
        if (mv == null || selection2 == null) return;

        //Layer? selectionLayer = _allCurrentLayers?.FirstOrDefault(l => l.Name == selection);
        Layer? selectionLayer = _allCurrentLayers?.FirstOrDefault(l => l.Name == selection2);
        FeatureLayer? selectionFeatureLayer = selectionLayer as FeatureLayer;
        mv.SelectLayers(new[] { selectionLayer });

        // This is the primary Working call to open the popup dock pane ...but doesnt create a new expression item-Opens configure popup pane.
        ProApp.DockPaneManager.Find("esri_mapping_popupsDockPane").Activate();
        //ProApp.Panes.Find("esri_mapping_popupsDockPane").FindModule("_ExpressionPageMainGrid")
        //ProApp.DockPaneManager.Find("_newExpressionsButton");
        //ExpressionBuilderPopupView 
        //new expression item - Opens configure popup pane.
        //var dockpane = ProApp.DockPaneManager.Find("esri_mapping_popupsDockPane");
        //var popupExpressionGrid = WindowHelpers.FindChild<Grid>(dockpane.Content as FrameworkElement, "_expressionsPageMainGrid");
        //
        //    dockpane.Activate();
        //    ActivateNewArcadeExpressionBtn();

    }

    List<FeatureLayer>? _allCurrentLayers = MapView.Active?.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private MessageSettings _settings = Module1.GetMessageSettings();
    
}
