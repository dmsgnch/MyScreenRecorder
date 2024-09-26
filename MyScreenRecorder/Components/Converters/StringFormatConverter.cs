using System;
using Windows.UI.Xaml.Data;

namespace MyScreenRecorder.Components.Converters;

public class StringFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
            return null;

        if (parameter is null)
            return value;

        return string.Format((string)parameter, value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}