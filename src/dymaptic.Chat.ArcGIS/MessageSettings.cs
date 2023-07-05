using ArcGIS.Desktop.Mapping;
using dymaptic.Chat.Shared.Data;

namespace dymaptic.Chat.ArcGIS;

public class MessageSettings
{
    public DyChatContext? DyChatContext { get; set; }
    public Layer? SelectedFeatureLayer { get; set; }
    public Layer? SelectedLayer { get; set; }
}

