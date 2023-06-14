using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// Represents the ComboBox
/// </summary>
public class LayerSelection : ComboBox
{

    /// <summary>
    /// Combo Box constructor
    /// </summary>
    public LayerSelection()
    {
        UpdateCombo();


    }

    
    /// <summary>
    /// Updates the combo box with all the items.
    /// </summary>

    private void UpdateCombo()
    {
        if (_isInitialized)
            SelectedItem = ItemCollection.FirstOrDefault(); //set the default item in the comboBox
        if (!_isInitialized)
        {
            Clear();
            //subscribe to events to populate snap layer list when the map changes, layers added/removed
            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
            if (MapView.Active != null)
            {
                OnActiveMapViewChanged(new ActiveMapViewChangedEventArgs(MapView.Active, null));
            }
            LayersAddedEvent.Subscribe(OnLayersAdd);
            LayersRemovedEvent.Subscribe(OnLayersRem);
            _isInitialized = true;
        }
        //set the default item in the comboBox
        SelectedItem = ItemCollection.FirstOrDefault();
    }
    private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
    {
        Clear();
        if (args.IncomingView != null)
        {
            var layerlist = args.IncomingView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>();
            //Add feature layer names to the combobox
            QueuedTask.Run(() =>
            {
                foreach (var layer in layerlist)
                {
                    Add(MakeComboBoxItem(layer.GetDefinition() as CIMFeatureLayer));
                }
            });
        }
    }

    private async void OnLayersAdd(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersadded event (which runs on background)
        var existingLayerNames = this.ItemCollection.Select(i => i.ToString());
        foreach (var addedLayer in args.Layers)
        {
            if (addedLayer is not FeatureLayer featureLayer) continue;
            if (!existingLayerNames.Contains(addedLayer.Name))
            {
                var comboItem = await QueuedTask.Run(() =>
                {
                    return MakeComboBoxItem(addedLayer.GetDefinition() as CIMFeatureLayer);
                });
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => { this.Add(comboItem); }));
            }
        }
    }
    private void OnLayersRem(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersadded event (which runs on background)
        var existingLayerNames = this.ItemCollection.Select(i => i.ToString());
        foreach (var removedLayer in args.Layers)
        {
            if (existingLayerNames.Contains(removedLayer.Name))
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => { this.Remove(removedLayer.Name); }));
            OnActiveMapViewChanged(new ActiveMapViewChangedEventArgs(MapView.Active, null));
        }
    }

    protected override void OnSelectionChange(ComboBoxItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.Text))
        {
            Module1.Current.SelectedDestinationFeatureLayer = string.Empty;

            return ;
        }
        var selectionResult = OnLayerSelection(item.Text);
        Module1.Current.SelectedDestinationFeatureLayer = $@"{item.Text}";
    }

    public async Task OnLayerSelection(string layer)
    {       
        List<DyLayer> layerList = new List<DyLayer>();
        List<DyField> layerFieldCollection = new List<DyField>();
        List<FeatureLayer>? allViewLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
        // Get the features that intersect the sketch geometry.
        await QueuedTask.Run(() =>
        {
            foreach (var viewLayer in allViewLayers)
            {
                var layerFields = viewLayer.GetFieldDescriptions();
                foreach (var field in layerFields)
                {
                    DyField dyField = new DyField(field.Name, field.Alias, field.Type.ToString());
                    layerFieldCollection.Add(dyField);
                }
                DyLayer dyLayer = new DyLayer(viewLayer.Name, layerFieldCollection);

                layerList.Add(dyLayer);
            }

            // build and return the dyChatContext object to send to settings
            DyChatContext dyChatContext = new DyChatContext(layerList, layer);

            _settings.DyChatContext = dyChatContext;
            _settings.CurrentLayer = layer;

            Module1.SaveSettings(_settings);
        });
    }

    static ComboBoxItem MakeComboBoxItem(CIMFeatureLayer cimFeatureLayer)
    {
        var toolTip = $@"Select this feature layer: {cimFeatureLayer.Name}";
        if (cimFeatureLayer.Renderer is not CIMSimpleRenderer cimRenderer)
        {
            return new ComboBoxItem(cimFeatureLayer.Name, null, toolTip);
        }
        var si = new SymbolStyleItem()
        {
            Symbol = cimRenderer.Symbol.Symbol,
            PatchHeight = 16,
            PatchWidth = 16
        };
        var bm = si.PreviewImage as BitmapSource;
        //bm.Freeze();
        return new ComboBoxItem(cimFeatureLayer.Name, bm, toolTip);
    }

    private bool _isInitialized;
    //private List<FeatureLayer>? _allViewLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
    private Settings _settings = Module1.GetSettings();
    //ObservableCollection<object> LayerCollection = new ObservableCollection<object>();
}



// want to enable multiple selections?
// once the user leaves the dropdown, the method should pop up a window for the user to verify the selections
// once the user clicks ok, the "extraction" method should run 
// ? the results of the extraction method goes where?