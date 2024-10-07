using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace MyScreenRecorder.Models;

public class AppViewRecord
{
    public Type WindowType { get; set; }
    public ApplicationView AppView { get; set; }
    public Frame WindowFrame { get; set; }

    public AppViewRecord()
    { }

    public AppViewRecord(Type windowType, ApplicationView appView, Frame windowFrame)
    {
        WindowType = windowType;
        AppView = appView;
        WindowFrame = windowFrame;
    }
}