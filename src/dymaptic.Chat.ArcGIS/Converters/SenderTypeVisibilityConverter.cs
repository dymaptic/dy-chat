using dymaptic.Chat.Shared.Data;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dymaptic.Chat.ArcGIS.Converters;

public class SenderTypeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Enum.TryParse<DyChatSenderType>(value?.ToString(), out var senderType))
        {
            return senderType == DyChatSenderType.Bot ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseSenderTypeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Enum.TryParse<DyChatSenderType>(value?.ToString(), out var senderType))
        {
            return senderType == DyChatSenderType.Bot ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
