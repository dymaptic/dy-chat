
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;

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
        if (mv == null || selection == null)
            return;
        else
        {
            
            Layer? selectionLayer = _allCurrentLayers?.FirstOrDefault(l => l.Name == selection);
            mv.SelectLayers(new[] { selectionLayer });

            ProApp.DockPaneManager.Find("esri_mapping_popupsDockPane").Activate();

            Console.WriteLine("Popup Configuration Text Block");
        }
    }

    List<FeatureLayer>? _allCurrentLayers = MapView.Active?.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private Settings _settings = Module1.GetSettings();
}
