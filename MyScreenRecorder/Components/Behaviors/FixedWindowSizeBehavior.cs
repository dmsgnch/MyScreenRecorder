using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace MyScreenRecorder.Components.Behaviors
{
    public class FixedWindowSizeBehavior : Behavior<Page>
    {
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public string Title { get; set; }
        
        public Size ViewSize { get; set; }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            
            Window.Current.CoreWindow.SizeChanged -= ResizeView;
        }

        protected override async void OnAttached()
        {
            base.OnAttached();

            SetWindowTitle();

            ViewSize = new Size(WindowWidth, WindowHeight);
            await SetWindowSizeAsync(ViewSize);
            CoreWindow.GetForCurrentThread().Activated += ResizeView;
            CoreWindow.GetForCurrentThread().Closed += delegate
            {
                CoreWindow.GetForCurrentThread().Activated -= ResizeView;
            };
            ApplicationView.GetForCurrentView().Consolidated += delegate
            {
                Window.Current.CoreWindow.SizeChanged -= ResizeView;
            };
            
            Window.Current.CoreWindow.SizeChanged += ResizeView;
        }

        private void ResizeView(CoreWindow window, WindowSizeChangedEventArgs args)
        {
            ApplicationView.GetForCurrentView().TryResizeView(ViewSize);
        }
        
        private void ResizeView(CoreWindow window, WindowActivatedEventArgs args)
        {
            ApplicationView.GetForCurrentView().TryResizeView(ViewSize);
        }

        private void SetWindowTitle()
        {
            ApplicationView.GetForCurrentView().Title = Title;
        }

        private async Task SetWindowSizeAsync(Size size)
        {
            await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var appView = ApplicationView.GetForCurrentView();
            
                appView.SetPreferredMinSize(size);
                if (!appView.TryResizeView(size)) throw new Exception("Current app view cant be resized!");
            });
        }
    }
}