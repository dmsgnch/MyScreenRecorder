using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using MyScreenRecorder.Commands;
using MyScreenRecorder.Services;
using MyScreenRecorder.Views;
using Xabe.FFmpeg.Downloader;

namespace MyScreenRecorder.ViewModels;

public class MainPageViewModel
{
    public RelayCommand StartRecordingCommand { get; }
    
    private bool ffmpegDownloading = false;

    private bool FFmpegDownloading
    {
        get => ffmpegDownloading;
        set
        {
            if (!value.Equals(ffmpegDownloading))
            {
                ffmpegDownloading = value;
                StartRecordingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public MainPageViewModel()
    {
        StartRecordingCommand = new RelayCommand((param) => StartRecordingAsync(), IsStartRecordingCanExecute);
        ApplicationData.Current.LocalSettings.Values["backgroundTaskName"] = "Recording";
    }

    private bool IsStartRecordingCanExecute() => !FFmpegDownloading;

    private async void StartRecordingAsync()
    {
        await DownloadFFmpegAsync();

        SetAppSettings();
        UnregisterBackgroundTaskIfRegistered();

        await RegisterBackgroundTaskAsync();

        await AppViewsLauncherService.ToggleToTargetTypeWindowWithSizeAsync(typeof(RecordingPage));
    }

    private async Task DownloadFFmpegAsync()
    {
        FFmpegDownloading = true;

        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official,
            ApplicationData.Current.LocalFolder.Path);

        FFmpegDownloading = false;
    }

    private void SetAppSettings()
    {
        string outputFileName = $"{Guid.NewGuid()}.mp4";

        ApplicationData.Current.LocalSettings.Values["arguments"] = GenerateArguments();
        ApplicationData.Current.LocalSettings.Values["outputFileName"] = outputFileName;
    }

    private string GenerateArguments()
    {
        //Will be extended in the near future
        return $"-f gdigrab -framerate 30 -i desktop -c:v libx264 -y";
    }

    private void UnregisterBackgroundTaskIfRegistered()
    {
        var recordingBackgroundTask = BackgroundTaskRegistration.AllTasks.Values
            .FirstOrDefault(i =>
                i.Name == (string)ApplicationData.Current.LocalSettings.Values["backgroundTaskName"]);

        recordingBackgroundTask?.Unregister(true);
    }

    private async Task RegisterBackgroundTaskAsync()
    {
        var backgroundTaskAppTrigger = new ApplicationTrigger();
        var recordingBackgroundTask = GetRegisteredTaskWithAppTrigger(backgroundTaskAppTrigger);

        recordingBackgroundTask.Completed += OnTask_Completed;

        await backgroundTaskAppTrigger.RequestAsync();
    }

    private BackgroundTaskRegistration GetRegisteredTaskWithAppTrigger(ApplicationTrigger applicationTrigger)
    {
        if (applicationTrigger is null) throw new ArgumentException("applicationTrigger must be not null");

        var taskBuilder = new BackgroundTaskBuilder();
        taskBuilder.Name = (string)ApplicationData.Current.LocalSettings.Values["backgroundTaskName"];
        taskBuilder.TaskEntryPoint = typeof(RecordingRuntimeComponent.RecordingBackgroundTask).ToString();

        taskBuilder.SetTrigger(applicationTrigger);
        return taskBuilder.Register();
    }

    private async void OnTask_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
    {
        await MoveFileToVideoFolderAsync();

        Debug.WriteLine("Recording completed");
    }

    private async Task MoveFileToVideoFolderAsync()
    {
        var outputFileName = (string)ApplicationData.Current.LocalSettings.Values["outputFileName"];

        var storageFolder =
            await KnownFolders.VideosLibrary.CreateFolderAsync(nameof(MyScreenRecorder),
                CreationCollisionOption.OpenIfExists);
        var storageOutputFile = await ApplicationData.Current.LocalFolder.GetFileAsync(outputFileName);
        storageOutputFile.MoveAsync(storageFolder);
    }
}