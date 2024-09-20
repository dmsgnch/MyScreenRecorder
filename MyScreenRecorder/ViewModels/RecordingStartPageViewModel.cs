using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using MyScreenRecorder.Commands;
using Xabe.FFmpeg.Downloader;

namespace MyScreenRecorder.ViewModels;

public class RecordingStartPageViewModel
{
    public RelayCommand StartRecordingCommand { get; }
    public RelayCommand StopRecordingCommand { get; }

    private string BackgroundTaskName { get; set; } = "Recording";

    public RecordingStartPageViewModel()
    {
        StartRecordingCommand = new RelayCommand((param) => StartRecordingAsync());
        StopRecordingCommand = new RelayCommand((param) => StopRecording());
    }

    private async void StartRecordingAsync()
    {
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official,
                    ApplicationData.Current.LocalFolder.Path);

        string arguments = $"-f gdigrab -framerate 30 -i desktop -c:v libx264 -y";
        string outputFileName = $"{Guid.NewGuid()}.mp4";
        
        ApplicationData.Current.LocalSettings.Values["arguments"] = arguments;
        ApplicationData.Current.LocalSettings.Values["outputFileName"] = outputFileName;
        
        
        var recordingBackgroundTask = BackgroundTaskRegistration.AllTasks.Values
            .FirstOrDefault(i => i.Name == BackgroundTaskName);
        if (recordingBackgroundTask is null)
        {
            var backgroundTaskAppTrigger = new ApplicationTrigger();
            recordingBackgroundTask = GetRegisteredTaskWithAppTrigger(backgroundTaskAppTrigger);

            recordingBackgroundTask.Completed += Task_Completed;

            await backgroundTaskAppTrigger.RequestAsync();
        }
    }

    private BackgroundTaskRegistration GetRegisteredTaskWithAppTrigger(ApplicationTrigger applicationTrigger)
    {
        if (applicationTrigger is null) throw new ArgumentException("applicationTrigger must be not null");
        
        var taskBuilder = new BackgroundTaskBuilder();
        taskBuilder.Name = BackgroundTaskName;
        taskBuilder.TaskEntryPoint = typeof(RecordingRuntimeComponent.RecordingBackgroundTask).ToString();
        
        taskBuilder.SetTrigger(applicationTrigger);
        return taskBuilder.Register();
    }

    private async void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
    {
        await MoveFileToVideoFolderAsync();
        
        Debug.WriteLine("Recording completed");
    }

    private async Task MoveFileToVideoFolderAsync()
    {
        var outputFileName = (string)ApplicationData.Current.LocalSettings.Values["outputFileName"];
        
        var storageFolder = await KnownFolders.VideosLibrary.CreateFolderAsync(nameof(MyScreenRecorder), CreationCollisionOption.OpenIfExists);
        var storageOutputFile = await ApplicationData.Current.LocalFolder.GetFileAsync(outputFileName);
        storageOutputFile.MoveAsync(storageFolder);
    }

    private void StopRecording()
    {
        var taskList = BackgroundTaskRegistration.AllTasks.Values;
        var task = taskList.FirstOrDefault(i => i.Name == BackgroundTaskName);
        if (task != null)
        {
            task.Unregister(true);
        }
    }
}