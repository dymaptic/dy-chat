
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Core.CIM;
using System.Windows;
using System.Windows.Media;


namespace dymaptic.Chat.ArcGIS;

public class PopupConfiguration : MapTool
{
    public PopupConfiguration()
    {
        OpenPopupConfiguration();
    }

    private async Task OpenPopupConfiguration()
    {
        //replicate get selected feature, input right click action, select configure popup action, new configure popup pane
        var mv = MapView.Active;
        if (_settings == null)
        {
            _settings = Module1.GetMessageSettings();
        }

        var selectionFeatureLayer = _settings?.SelectedFeatureLayer;

        if (mv == null || selectionFeatureLayer == null) return;

        mv.SelectLayers(new[] { selectionFeatureLayer });

        FrameworkApplication.DockPaneManager.Find("esri_mapping_popupsDockPane").Activate();
    }

    //List<FeatureLayer>? _allCurrentLayers = MapView.Active?.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private MessageSettings _settings = Module1.GetMessageSettings();
    
}
