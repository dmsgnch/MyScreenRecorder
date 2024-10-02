using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using MyScreenRecorder.Commands;
using MyScreenRecorder.Services;
using MyScreenRecorder.Views;

namespace MyScreenRecorder.ViewModels;

public class RecordingPageViewModel : INotifyPropertyChanged
{
    #region Commands

    public RelayCommandAsync StopRecordingAsyncCommand { get; }

    #endregion

    #region Binding params

    private string videoDuration = "";

    public string VideoDuration
    {
        get => videoDuration;
        set
        {
            if (!value.Equals(videoDuration))
            {
                videoDuration = value;
                OnPropertyChanged();
            }
        }
    }

    private string videoSize = "";

    public string VideoSize
    {
        get => videoSize;
        set
        {
            if (!value.Equals(videoSize))
            {
                videoSize = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    private CoreDispatcher CoreDispatcher { get; }
    private bool Reading { get; set; } = true;

    public RecordingPageViewModel()
    {
        StopRecordingAsyncCommand = new RelayCommandAsync(async (param) => await StopRecordingAsync());

        CoreDispatcher = CoreApplication.GetCurrentView().Dispatcher;
        CreateServerPipeStream();
    }

    private async void CreateServerPipeStream()
    {
        await Task.Run(async () =>
        {
            using (var server = new NamedPipeServerStream(@"LOCAL\MYPIPE"))
            {
                await server.WaitForConnectionAsync();

                using (var reader = new StreamReader(server))
                {
                    while (Reading)
                    {
                        string message = await reader.ReadLineAsync();

                        SetNewRecordingDataOrDefault(message);
                    }
                }
            }
        });
    }
    
    #region Updating binding params

    private void SetNewRecordingDataOrDefault(string? infoLine)
    {
        VideoDuration = GetTimeFromStringOrDefault(infoLine) ?? "00:00:00.00";
        VideoSize = GetSizeFromStringOrDefault(infoLine) ?? "N/A";
    }

    private string? GetTimeFromStringOrDefault(string? infoLine)
    {
        string? timeString = null;

        if (infoLine is not null)
        {
            var timeRegex = new Regex(@"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
            var timeMatch = timeRegex.Match(infoLine);

            if (timeMatch.Success)
            {
                timeString = timeMatch.Groups[1].Value;
            }
        }

        return timeString;
    }
    
    private string? GetSizeFromStringOrDefault(string? infoLine)
    {
        string? sizeString = null;
        
        if (infoLine is not null)
        {
            var sizeRegex = new Regex(@"size=\s*(\d+kB)");
            var sizeMatch = sizeRegex.Match(infoLine);

            if (sizeMatch.Success)
            {
                sizeString = sizeMatch.Groups[1].Value;
            }
        }

        return sizeString;
    }
    
    #endregion

    private async Task StopRecordingAsync()
    {
        var taskList = BackgroundTaskRegistration.AllTasks.Values;
        var task = taskList.FirstOrDefault(i =>
            i.Name == (string)ApplicationData.Current.LocalSettings.Values["backgroundTaskName"]);
        if (task != null)
        {
            Reading = false;
            task.Unregister(true);

            await AppViewsLauncherService.ToggleToTargetTypeWindowWithSizeAsync(typeof(MainPage));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        CoreDispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () => { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
    }
}