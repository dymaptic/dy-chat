using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
        {
            SelectedItem = ItemCollection.FirstOrDefault();
        }
        else
        {
            Clear();
            // subscriptions to 3 events that will affect the combobox: ActiveMapViewChanged, LayersAdded, LayersRemoved
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

    /// <summary>
    /// Tracks changes in the active mapview as the source for other adjustments that may occur for example
    /// Layers added/removed in the table of contents.
    /// </summary>
    private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
    {
        Clear();
        if (args.IncomingView != null)
        {
            var layerlist = args.IncomingView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>();
            _allViewLayers = layerlist.ToList();
            //Add feature layer names to the combobox
            QueuedTask.Run(() =>
            {
                foreach (var layer in _allViewLayers)
                {
                    Add(MakeComboBoxItem(layer.GetDefinition() as CIMFeatureLayer));
                }
            });
        }
    }

    /// <summary>
    /// Tracks when layers are added to the table of contents and then reflects that in the combobox values
    /// </summary>
    private async void OnLayersAdd(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersadded event (which runs on background)
        var existingLayerNames = ItemCollection.Select(i => i.ToString()).ToList();
        foreach (var addedLayer in args.Layers)
        {
            if (addedLayer is not FeatureLayer featureLayer) continue;
            if (!existingLayerNames.Contains(addedLayer.Name))
            {
                var comboItem = await QueuedTask.Run(() =>
                    MakeComboBoxItem(addedLayer.GetDefinition() as CIMFeatureLayer));

                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => { this.Add(comboItem); }));
            }
        }
    }
    /// <summary>
    /// Tracks when layers are removed to the table of contents and then reflects that in the combobox values
    /// </summary>
    private void OnLayersRem(LayerEventsArgs args)
    {
        //run on UI Thread to sync layersadded event (which runs on background)
        var existingLayerNames = this.ItemCollection.Select(i => i.ToString()).ToList();
        foreach (var removedLayer in args.Layers)
        {
            if (existingLayerNames.Contains(removedLayer.Name))
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => { this.Remove(removedLayer.Name); }));
            OnActiveMapViewChanged(new ActiveMapViewChangedEventArgs(MapView.Active, null));
        }
    }

    protected override void OnSelectionChange(ComboBoxItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Text))
        {
            Module1.Current.SelectedDestinationFeatureLayer = string.Empty;

            return;
        }

        var selectionResult = OnLayerSelection(item.Text);

        Module1.Current.SelectedDestinationFeatureLayer = $@"{item.Text}";
    }

    /// <summary>
    /// Tracks when layers are removed to the table of contents and then reflects that in the combobox values
    /// </summary>
    public async Task OnLayerSelection(string layer)
    {
        List<DyLayer> layerList = new List<DyLayer>();
        List<DyField> layerFieldCollection = new List<DyField>();

        // Get the features that intersect the sketch geometry.
        await QueuedTask.Run(() =>
        {
            foreach (var viewLayer in _allViewLayers!)
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
            return layer;
        });
    }

    static ComboBoxItem MakeComboBoxItem(CIMFeatureLayer? cimFeatureLayer)
    {
        var toolTip = $@"Select this feature layer: {cimFeatureLayer?.Name}";
        if (cimFeatureLayer?.Renderer is not CIMSimpleRenderer cimRenderer)
        {
            return new ComboBoxItem(cimFeatureLayer?.Name, null, toolTip);
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
    List<FeatureLayer>? _allViewLayers;
    private Settings _settings = Module1.GetSettings();

}



