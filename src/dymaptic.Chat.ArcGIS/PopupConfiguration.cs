using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Core.Data;
using ArcGIS.Core.CIM;
//using ArcGIS.Desktop.Internal.Framework.Behaviors;
using System.Windows.Controls.Primitives;
//using ArcGIS.Desktop.Internal.Framework;

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
        if (mv == null)
            return;
        
        var selection = _settings.DyChatContext.CurrentLayer;
        FeatureLayer? selectionLayer = _allCurrentLayers?.FirstOrDefault(l => l.Name == selection);

        var mapPanesList = ProApp.Panes;
        var mapPanes = ProApp.Panes.Create("esri_editing_Attributes_OpenPopupSelectionContextMenuItem");
        mapPanes.Activate();

        Console.WriteLine("MapPane Activated");

        var selectionResults = await QueuedTask.Run(() =>
        {
            
            FeatureClass selectionClass = selectionLayer.GetFeatureClass();
            Selection currentSelection = selectionLayer.GetSelection();
            selectionLayer.SetSelection(currentSelection);
            var selectedLayerDefinition = selectionLayer.GetDefinition() as CIMFeatureLayer;
            return selectionLayer;
        });

        var popupMenu = await QueuedTask.Run(() =>
        {
            var popupMenu = ProApp.DockPaneManager.Find("esri_editing_Attributes_OpenPopupSelectionContextMenuItem");
            if (popupMenu == null)
            ProApp.Panes.Create("esri_editing_Attributes_OpenPopupSelectionContextMenuItem", selectionLayer).Activate();
            
            return true;
        });
        Console.WriteLine("popupMenu is active");

    }

    private async void CreateSelectionSet(FeatureLayer? selectionLayer)
    {
        // cref: ArcGIS.Desktop.Mapping.Map.SetSelection(ArcGIS.Desktop.Mapping.SelectionSet, ArcGIS.Desktop.Mapping.SelectionCombinationMethod)
        {
            //FeatureClassSelect(selectionLayer);

            var activeSelection = FrameworkApplication.ContextMenuDataContextAs<SelectionSet>;
            _ = FrameworkApplication.ExecuteCommand("esri_mapping_popup_configure_popup");

            Console.WriteLine("Configure Popup menu open");
        }
    }

    List<FeatureLayer>? _allCurrentLayers = MapView.Active?.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private Settings _settings = Module1.GetSettings();
}
