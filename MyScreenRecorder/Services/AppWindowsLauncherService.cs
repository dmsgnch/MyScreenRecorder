using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using MyScreenRecorder.Models;

namespace MyScreenRecorder.Services;

public static class AppWindowsLauncherService
{
    private static List<AppWindowRecord> AppWindows { get; set; } = new();

    internal static async Task OpenAdditionalWindowAsync(Type targetWindowType)
    {
        var appWindowRecord = AppWindows.FirstOrDefault(aw => aw.WindowType == targetWindowType);
        if (appWindowRecord is null)
        {
            await OpenNewAppWindowAsync(targetWindowType);
        }
    }
    
    private static async Task OpenNewAppWindowAsync(Type typeOfWindow)
    {
        AppWindow? appWindow = await AppWindow.TryCreateAsync();

        Frame appWindowContentFrame = new Frame();
        appWindowContentFrame.Navigate(typeOfWindow);

        ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);

        AppWindows.Add(new AppWindowRecord(typeOfWindow, appWindow, appWindowContentFrame));

        appWindow.Closed += delegate
        {
            AppWindows.Remove(AppWindows.Find(aw => aw.WindowType == typeOfWindow));
            
            appWindowContentFrame.Content = null;
            appWindow = null;
        };

        await appWindow.TryShowAsync();
    }

    public static void ResizeWindowType(Type winType, Size size)
    {
        var appWindowRecord = AppWindows.FirstOrDefault(aw => aw.WindowType == winType);
        if (appWindowRecord is not null)
        {
            appWindowRecord.AppWindow.RequestSize(size);
        }
    }
    
    public static void SetWindowTitle(Type winType, string title)
    {
        var appWindowRecord = AppWindows.FirstOrDefault(aw => aw.WindowType == winType);
        if (appWindowRecord is not null)
        {
            appWindowRecord.AppWindow.Title = title;
        }
    }
    
    public static XamlRoot? GetXamlRootForWinType(Type winType)
    {
        var appWindowRecord = AppWindows.FirstOrDefault(aw => aw.WindowType == winType);
        return appWindowRecord?.WindowFrame.XamlRoot;
    }

    public static void ReloadAllOpenWindows()
    {
        foreach (var appWindow in AppWindows)
        {
            appWindow.WindowFrame.Navigate(appWindow.WindowFrame.Content.GetType());
        }
    }
}