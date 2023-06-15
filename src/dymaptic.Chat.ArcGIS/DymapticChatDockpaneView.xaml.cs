using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Themes;
using ArcGIS.Desktop.Framework;


namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// Interaction logic for DymapticChatDockpaneView.xaml
/// </summary>
public partial class DymapticChatDockpaneView : UserControl
{
    public DymapticChatDockpaneView()
    {
        InitializeComponent();

        //Gets the application's theme
        var theme = FrameworkApplication.ApplicationTheme;

        var dictionary = new ResourceDictionary();
        //ApplicationTheme enumeration
        //Dark theme
        if (FrameworkApplication.ApplicationTheme == ApplicationTheme.Dark)
        {
            dictionary.Source = new System.Uri("pack://application:,,,/dymaptic.Chat.ArcGIS;component/Themes/DarkTheme.xaml");
        }
        //Light theme
        //contrast theme?
        else
        {
            dictionary.Source = new System.Uri("pack://application:,,,/dymaptic.Chat.ArcGIS;component/Themes/LightTheme.xaml");
        }
        this.Resources.MergedDictionaries.Add(dictionary);
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
