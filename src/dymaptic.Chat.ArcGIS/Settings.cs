using ArcGIS.Desktop.Mapping;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Chat.ArcGIS;

public class Settings
{
    public DyChatContext DyChatContext { get;set; }
    public string CurrentLayer { get;set; }   
}

