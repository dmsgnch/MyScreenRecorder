using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;
using MyScreenRecorder.Commands;
using MyScreenRecorder.Services;
using MyScreenRecorder.Views;
using Xabe.FFmpeg.Downloader;

namespace MyScreenRecorder.ViewModels;

public class MainPageViewModel
{
    public RelayCommand StartRecordingCommand { get; }
    public RelayCommandAsync OpenRecordsWindowAsyncCommand { get; }

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
        OpenRecordsWindowAsyncCommand = new RelayCommandAsync(async (param) => await OpenRecordsWindowAsync());

        ApplicationData.Current.LocalSettings.Values["backgroundTaskName"] = "Recording";
    }

    #region Start recording command functionality

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
        await AddThumbnailToVideo();
        await MoveFileToVideoFolderAsync();

        Debug.WriteLine("Recording completed");
    }

    private async Task AddThumbnailToVideo()
    {
        var recordingOutputFileName = (string)ApplicationData.Current.LocalSettings.Values["outputFileName"];
        var recordingOutputFile = await FileAccessService.GetFileFromLocalStorageByNameAsync(recordingOutputFileName);

        //Prepare paths for the new files 
        string ffmpegThumbnail = FileAccessService.GetPathFromLocalStorageByName("ThumbnailTemp.jpg");
        string outputFileWithThumbnail = FileAccessService.GetPathFromLocalStorageByName(recordingOutputFileName);

        await recordingOutputFile.RenameAsync("TempRecord.mp4", NameCollisionOption.ReplaceExisting);

        //Form commands for ffmpeg
        var getThumbnailArguments = $"-i {recordingOutputFile.Path} -ss 00:00:00 -vframes 1 -y {ffmpegThumbnail}";
        var setThumbnailArguments =
            $"-i {recordingOutputFile.Path} -i {ffmpegThumbnail} -map 0 -map 1 -c copy -disposition:v:1 attached_pic -y {outputFileWithThumbnail}";

        string ffmpegFilePath = FileAccessService.GetPathFromLocalStorageByName("ffmpeg.exe");
        var ffmpegLauncher = new FFmpegProcessLauncher(ffmpegFilePath);

        ffmpegLauncher.StartSimpleProcess(getThumbnailArguments);
        ffmpegLauncher.StartSimpleProcess(setThumbnailArguments);

        //Delete temporary files
        await recordingOutputFile.DeleteAsync();
        var picFile = await StorageFile.GetFileFromPathAsync(ffmpegThumbnail);
        await picFile.DeleteAsync();
    }

    private async Task MoveFileToVideoFolderAsync()
    {
        var outputFileName = (string)ApplicationData.Current.LocalSettings.Values["outputFileName"];

        var storageFolder =
            await KnownFolders.VideosLibrary.CreateFolderAsync(nameof(MyScreenRecorder),
                CreationCollisionOption.OpenIfExists);
        var storageOutputFile = await FileAccessService.GetFileFromLocalStorageByNameAsync(outputFileName);
        storageOutputFile.MoveAsync(storageFolder);
    }

    #endregion

    #region Open records window command functionality

    private async Task OpenRecordsWindowAsync()
    {
        await AppWindowsLauncherService.OpenAdditionalWindowAsync(typeof(RecordsListPage));
        AppWindowsLauncherService.ResizeWindowType(typeof(RecordsListPage), new Size(1024, 768));
        AppWindowsLauncherService.SetWindowTitle(typeof(RecordsListPage), "Records list");
    }

    #endregion
}