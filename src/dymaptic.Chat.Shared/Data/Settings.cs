using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Chat.Shared.Data;

public class Settings
{
    public DyField DyField { get;set; }
    public DyLayer DyLayer { get;set; }
    public DyChatContext DyChatContext { get;set; }
    public string CurrentLayer { get;set; }
    public List<FeatureLayer> CatalogLayerList { get;set; }
}

