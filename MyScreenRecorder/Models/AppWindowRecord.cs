using System;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;

namespace MyScreenRecorder.Models;

public class AppWindowRecord
{
    public Type WindowType { get; set; }
    public AppWindow AppWindow { get; set; }
    public Frame WindowFrame { get; set; }

    public AppWindowRecord()
    { }

    public AppWindowRecord(Type windowType, AppWindow appWindow, Frame windowFrame)
    {
        WindowType = windowType;
        AppWindow = appWindow;
        WindowFrame = windowFrame;
    }
}