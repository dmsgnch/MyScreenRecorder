using System;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;

namespace MyScreenRecorder.Models;

public class Record
{
    public string FileName { get; set; }
    public string CreationData { get; set; }

    public BitmapImage PreviewImage { get; set; } = new();

    public Record()
    { }

    public Record(string fileName, DateTimeOffset creationData, BitmapImage previewImage)
    {
        FileName = fileName;
        CreationData = creationData.ToString("MM/dd/yy h:mm:ss tt", CultureInfo.InvariantCulture);
        PreviewImage = previewImage;
    }
}