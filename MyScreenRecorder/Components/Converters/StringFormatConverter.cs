using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace MyScreenRecorder.Components.Converters;

public class StringFormatConverter : IValueConverter
{
    private readonly ResourceLoader resourceLoader;
    
    public StringFormatConverter()
    {
        resourceLoader = ResourceLoader.GetForCurrentView("RecordingWindow");
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
            return null;

        if (parameter is null)
            return value;

        string formatString;
        
        if (parameter is string parameterString)
        {
            formatString = resourceLoader.GetString(parameterString);
        }
        else
        {
            return value;
        }
        
        return $"{formatString}: {value}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}