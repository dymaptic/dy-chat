using dymaptic.Chat.Shared.Data;
using System.Windows.Controls;


namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// Interaction logic for DymapticChatDockpaneView.xaml
/// </summary>
public partial class DymapticChatDockpaneView : UserControl
{
    public DymapticChatDockpaneView()
    {
        InitializeComponent();
        Module1.GetSettings();
    }

   

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer scrollViewer = (ScrollViewer)sender;
        if (e.ExtentHeightChange != 0)
        {
            scrollViewer.ScrollToEnd();
        }
    }

}
