using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MyScreenRecorder.Models;

namespace MyScreenRecorder.Services;

internal static class AppViewsLauncherService
{
    private static List<AppViewRecord> ApplicationViews { get; set; } = new();
    private static Type? StartWindowType { get; set; }

    internal static void InitAppView(AppViewRecord appViewRecord, Size size)
    {
        if (!ApplicationViews.Count.Equals(0))
            throw new InvalidOperationException("Start ApplicationView has already been initialized!");

        StartWindowType = appViewRecord.WindowType;
        ApplicationViews.Add(appViewRecord);

        ApplicationView.GetForCurrentView().TryResizeView(size);
    }

    internal static async Task ToggleToTargetTypeWindowWithSizeAsync(Type targetWindowType)
    {
        if (StartWindowType is null)
            throw new InvalidOperationException("StartWindowType must be initialized before windows toggle!");

        var record = ApplicationViews.FirstOrDefault(r => r.WindowType == targetWindowType);
        if (record is not null)
        {
            await SwitchToExistViewAsync(record.AppView.Id);
        }
        else
        {
            await OpenNewAppViewAsync(targetWindowType);
        }
    }

    private static async Task SwitchToExistViewAsync(int appViewId)
    {
        int currentId = ApplicationView.GetForCurrentView().Id;
        var window = Window.Current;

        await ApplicationViewSwitcher.SwitchAsync(appViewId, currentId, ApplicationViewSwitchingOptions.SkipAnimation);

        //If current window is start window we shouldn`t close it
        var record = ApplicationViews.FirstOrDefault(r => r.WindowType == StartWindowType);
        if (record is null || !record.AppView.Id.Equals(currentId))
        {
            window.Close();
        }
    }

    private static async Task OpenNewAppViewAsync(Type typeOfWindow)
    {
        CoreApplicationView newView = CoreApplication.CreateNewView();
        var currentViewId = ApplicationView.GetForCurrentView().Id;

        int newViewId = 0;
        await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            Frame frame = new Frame();
            frame.Navigate(typeOfWindow, null);
            Window.Current.Content = frame;
            Window.Current.Activate();

            newViewId = ApplicationView.GetForCurrentView().Id;

            Window.Current.Closed += delegate
            {
                ApplicationViews.Remove(ApplicationViews.Find(avr => avr.WindowType == typeOfWindow));
            };
            
            ApplicationViews.Add(new AppViewRecord(typeOfWindow, ApplicationView.GetForCurrentView(), frame));
        });

        await ApplicationViewSwitcher.SwitchAsync(newViewId, currentViewId,
            ApplicationViewSwitchingOptions.SkipAnimation);
    }
    
    public static Frame GetCurrentAppViewFrame()
    {
        var currentView = ApplicationView.GetForCurrentView();
        return ApplicationViews.Find(aw => aw.AppView == currentView).WindowFrame;
    }
}