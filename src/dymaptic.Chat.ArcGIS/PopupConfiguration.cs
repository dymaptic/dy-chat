using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Chat.ArcGIS;

public class PopupConfiguration : MapTool
{
    public PopupConfiguration()
    {
        OpenPopupConfiguration();
    }

    private void OpenPopupConfiguration()
    {
        //replicate get selected feature, input right click action, select configure popup action, new popup instance
        var mv = MapView.Active;
        if (mv == null)
            return;
        //_ = System.Windows.Application.Current.FindResource("esri_mapping") as System.Windows.ResourceDictionary;
        //var menu = new System.Windows.Controls.ContextMenu();

        var selectionLayer = _settings.DyChatContext.CurrentLayer;
        _ = ArcGIS.Utils.GetICommand("esri_mapping_popup_configure_popup");
    }

    List<FeatureLayer>? _allContextLayers; 
    private Settings _settings = Module1.GetSettings();
}
