using ArcGIS.Core.CIM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace dymaptic.Chat.ArcGIS.Converters
{
    public class LayerIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //this is a hack because the layer info needs to run with the queued task.
            //converters probably should not need to run things async
            return Task.Run(async () =>
                await QueuedTask.Run(() =>
                {
                    if (value is not BasicFeatureLayer layer)
                    {
                        return null;
                    }
                    var cimFeatureLayer = layer.GetDefinition() as CIMFeatureLayer;
                    if (cimFeatureLayer?.Renderer is not CIMSimpleRenderer cimRenderer)
                    {
                        return null;
                    }

                    var si = new SymbolStyleItem()
                    {
                        Symbol = cimRenderer.Symbol.Symbol,
                        PatchHeight = 16,
                        PatchWidth = 16
                    };
                    var bm = si.PreviewImage as BitmapSource;
                    return bm;
                })).Result!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
