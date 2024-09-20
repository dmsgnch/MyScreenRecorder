using Windows.Foundation;
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


        protected override void OnAttached()
        {
            base.OnAttached();
            
            SetFixedWindowSize(WindowWidth, WindowHeight);
            
            var size = new Size(WindowWidth, WindowHeight);
            Window.Current.CoreWindow.SizeChanged += (s, e) => { ApplicationView.GetForCurrentView().TryResizeView(size); };
        }

        private void SetFixedWindowSize(double width, double height)
        {
            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.TryResizeView(new Windows.Foundation.Size(width, height));
            applicationView.Title = Title;
            applicationView.FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
            applicationView.SetPreferredMinSize(new Windows.Foundation.Size(width, height));
        }
    }
}