using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace dymaptic.Chat.ArcGIS.Converters
{
    public class LayerIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                if (values[0] is Layer layer && values[1] is Dictionary<Layer, BitmapSource> iconDictionary
                                             && iconDictionary.TryGetValue(layer, out var value))
                    return value!;
            }
            return null!;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
