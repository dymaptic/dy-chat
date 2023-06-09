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
using dymaptic.Chat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace dymaptic.Chat.ArcGIS;

/// <summary>
/// Represents the ComboBox
/// </summary>
public class LayerSelection : ComboBox
{

    private bool _isInitialized;

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
        // TODO – customize this method to populate the combobox with your desired items

        if (_isInitialized)
            SelectedItem = ItemCollection.FirstOrDefault(); //set the default item in the comboBox



        if (!_isInitialized)
        {
            Clear();
            _allViewLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            //Add 6 items to the combobox
            foreach (var lyr in _allViewLayers)
            {
                string name = lyr.Name;
                Add(new ComboBoxItem(name));
            }
            _isInitialized = true;
        }


        Enabled = true; //enables the ComboBox

        Console.WriteLine(SelectedItem);
    }

    /// <summary>
    /// The on comboBox selection change event. 
    /// </summary>
    /// <param name="item">The newly selected combo box item</param>
    protected override void OnSelectionChange(ComboBoxItem item)
    {

        if (item == null)
            return;

        if (string.IsNullOrEmpty(item.Text))
            return;

        Console.WriteLine(item.Text);
        var selectionResult = OnLayerSelection(item.Text);
        // TODO  Code behavior when selection changes.

    }

    public async Task<string> OnLayerSelection(string layer)
    {
        var mv = MapView.Active;
        List<DyLayer> layerList = new List<DyLayer>();
        List<DyField> layerFieldCollection = new List<DyField>();
        string selectedLayer = "";
        string layerListOutput = "";
        bool selectionApproved = false;
        // Get the features that intersect the sketch geometry.

        // Get all layer definitions.
        foreach (var viewLayer in _allViewLayers)
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
        // Get the selected layer.
        if (layer == "")
        {
            MessageBox.Show($"Please select a layer");
            
            return null;
        }
        // build and return the dyChatContext object to send to settings
        DyChatContext dyChatContext = new DyChatContext(layerList, layer);
        return dyChatContext.ToString();
    }

    private List<FeatureLayer>? _allViewLayers;
    
}



// want to enable multiple selections?
// once the user leaves the dropdown, the method should pop up a window for the user to verify the selections
// once the user clicks ok, the "extraction" method should run 
// ? the results of the extraction method goes where?