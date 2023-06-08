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
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dymaptic.Chat.ArcGIS
{
    public class FeatureLayerFieldService 
    {

    }

    /// <summary>
    /// The identify Tool is the built in tool from an addin sample that allows the user to select feature that are active on the argis map.  
    /// </summary>
    internal class IdentifyTool : MapTool
    {
        public IdentifyTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Circle;
            SketchOutputMode = SketchOutputMode.Screen;
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        /// <summary>
        /// Inside this method is the mechanics to identify the layers and fields inside the selection area and then convert them to a json object. 
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            var mv = MapView.Active;
            List<DyLayer> layerList = new List<DyLayer>();
            List<DyField> layerFieldCollection = new List<DyField>();

            var identifyResult = await QueuedTask.Run(() =>
            {
                // Get the features that intersect the sketch geometry.
                var features = mv.GetFeatures(geometry);

                // Get all layer definitions.
                var lyrs = mv.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>();
                foreach (var lyr in lyrs)
                {
                    var layerFields = lyr.GetFieldDescriptions();
                    var fieldoutput2 = JsonSerializer.Serialize(layerFields);
                    foreach (var field in layerFields)
                    {
                        DyField dyField = new DyField(field.Name, field.Alias, field.Type.ToString());
                        layerFieldCollection.Add(dyField);
                    }
                    DyLayer dyLayer = new DyLayer(lyr.Name, layerFieldCollection);
                    var dyLayerOutput = JsonSerializer.Serialize(dyLayer);
                    layerList.Add(dyLayer);
                }
                var layerListOutput = JsonSerializer.Serialize(layerList);
                var json = layerListOutput.ToString();
                Console.WriteLine(layerListOutput);
                return layerListOutput;
            });
            //This output needs to be refactored to allow the user to copy the json object to the clipboard...or somehow copy the json object to the chat.

            MessageBox.Show($"Layer(s) Schema(s): {identifyResult}");
            return true;
        }
    }
    /// <summary>
    /// Borrowed from the dy-chat project
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Fields"></param>
    public record DyLayer(string Name, List<DyField> Fields);
    public record DyField(string Name, string Alias, string DataType);
}
