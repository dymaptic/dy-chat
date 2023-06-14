using dymaptic.Chat.Shared.Data;
using System.Windows.Controls;
using dymaptic.Chat.Shared.Data;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Linq;
using System;

namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// Interaction logic for DymapticChatDockpaneView.xaml
/// </summary>
public partial class DymapticChatDockpaneView : UserControl
{
    public DymapticChatDockpaneView()
    {
        InitializeComponent();
        
    }

   

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer scrollViewer = (ScrollViewer)sender;
        if (e.ExtentHeightChange != 0)
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private Settings _settings;
}
