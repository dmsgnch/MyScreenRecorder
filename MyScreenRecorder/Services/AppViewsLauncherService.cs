using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyScreenRecorder.Services;

internal static class AppViewsLauncherService
{
    private static Dictionary<Type, int> ApplicationViews { get; set; } = new();
    private static Type? StartWindowType { get; set; }
    
    internal static void InitAppView(Type frameType, int appViewId, Size size)
    {
        if (!ApplicationViews.Count.Equals(0)) 
            throw new InvalidOperationException("Start ApplicationView has already been initialized!");

        StartWindowType = frameType;
        ApplicationViews.Add(frameType, appViewId);
        
        ApplicationView.GetForCurrentView().TryResizeView(size);
    }
    
    internal static async Task ToggleToTargetTypeWindowWithSizeAsync(Type targetWindowType)
    {
        if (StartWindowType is null) 
            throw new InvalidOperationException("StartWindowType must be initialized before windows toggle!");
        
        if (ApplicationViews.TryGetValue(targetWindowType, out int appViewId))
        {
            await SwitchToExistViewAsync(appViewId);
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
        if (!ApplicationViews.TryGetValue(StartWindowType!, out int startViewId) || !startViewId.Equals(currentId))
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

            Window.Current.Closed += delegate { ApplicationViews.Remove(typeOfWindow); };
        });

        await ApplicationViewSwitcher.SwitchAsync(newViewId, currentViewId,
            ApplicationViewSwitchingOptions.SkipAnimation);
        
        ApplicationViews.Add(typeOfWindow, newViewId);
    }
}