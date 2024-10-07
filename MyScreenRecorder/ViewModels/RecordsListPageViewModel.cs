using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using MyScreenRecorder.Commands;
using MyScreenRecorder.Models;
using MyScreenRecorder.Services;
using MyScreenRecorder.Views;

namespace MyScreenRecorder.ViewModels;

public class RecordsListPageViewModel : INotifyPropertyChanged
{
    #region Commands

    public RelayCommandAsync UpdateRecordListCommand { get; }
    public RelayCommandAsync PlayRecordAsyncCommand { get; }
    public RelayCommandAsync OpenRecordFolderAsyncCommand { get; }
    public RelayCommandAsync RenameRecordAsyncCommand { get; }
    public RelayCommandAsync DeleteRecordAsyncCommand { get; }

    #endregion

    #region Binding params

    private ObservableCollection<Record> records;

    public ObservableCollection<Record> Records
    {
        get => records;
        set
        {
            if (!value.Equals(records))
            {
                records = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion
    
    public RecordsListPageViewModel()
    {
        UpdateRecordListCommand = new RelayCommandAsync(async (param) => await UpdateRecordsAsync());
        
        PlayRecordAsyncCommand = new RelayCommandAsync(async (param) => await PlayRecord((string)param));
        OpenRecordFolderAsyncCommand = new RelayCommandAsync(async (param) => await OpenRecordFolder());
        RenameRecordAsyncCommand = new RelayCommandAsync(async (param) => await RenameRecord((Record)param));
        DeleteRecordAsyncCommand = new RelayCommandAsync(async (param) => await DeleteRecord((Record)param));

        UpdateRecordListCommand.Execute(null);
    }

    private async Task UpdateRecordsAsync()
    {
        var storageFolder = await KnownFolders.VideosLibrary.GetFolderAsync(nameof(MyScreenRecorder));
        var files = await storageFolder.GetFilesAsync(CommonFileQuery.OrderByDate);
        
        var mp4Files = files.Where(file => file.FileType.Equals(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();
        
        List<Record> recordsTemp = new();
        foreach (var mp4File in mp4Files)
        {
            recordsTemp.Add(new Record(mp4File.Name, mp4File.DateCreated, await GetVideoThumbnailAsync(mp4File)));
        }

        Records = new ObservableCollection<Record>(recordsTemp);
    }

    public async Task<BitmapImage> GetVideoThumbnailAsync(StorageFile mp4File)
    {
        var image = await mp4File.GetThumbnailAsync(ThumbnailMode.VideosView, 100, ThumbnailOptions.UseCurrentScale);
        if (image is null) throw new ArgumentNullException(nameof(image));

        BitmapImage bitmapImage = new();
        await bitmapImage.SetSourceAsync(image);

        return bitmapImage;
    }

    private async Task PlayRecord(string recordFileName)
    {
        var storageFolder = await KnownFolders.VideosLibrary.GetFolderAsync(nameof(MyScreenRecorder));
        var mp4File = await storageFolder.GetFileAsync(recordFileName);

        if (mp4File is null) throw new ArgumentNullException(nameof(mp4File));
        Launcher.LaunchFileAsync(mp4File);
    }

    private async Task OpenRecordFolder()
    {
        var storageFolder = await KnownFolders.VideosLibrary.GetFolderAsync(nameof(MyScreenRecorder));
        Launcher.LaunchFolderAsync(storageFolder);
    }

    private async Task RenameRecord(Record record)
    {
        ContentDialog dialog = new ContentDialog
        {
            XamlRoot = AppWindowsLauncherService.GetXamlRootForWinType(typeof(RecordsListPage)),
            Title = "Enter new file name",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel"
        };
        
        TextBox inputTextBox = new TextBox
        {
            PlaceholderText = "New file name",
            Text = record.FileName
        };
        dialog.Content = inputTextBox;
        
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            string newFileName = inputTextBox.Text;

            if (IsValidFileName(newFileName))
            {
                record.FileName = newFileName;
                await UpdateRecordListCommand.ExecuteAsync(null);
            }
        }
    }
    
    private bool IsValidFileName(string fileName)
    {
        return FileNameNotContainInvalidChars(fileName) && IsFileTypeMp4(fileName);
    }

    private bool FileNameNotContainInvalidChars(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        bool isCorrect = true;
        
        foreach (char c in invalidChars)
        {
            if (fileName.Contains(c))
            {
                isCorrect = false;
            }
        }
        
        return isCorrect;
    }
    
    private bool IsFileTypeMp4(string fileName)
    {
        return fileName.EndsWith(".mp4");
    }

    private async Task DeleteRecord(Record record)
    {
        ContentDialog renameDialog = new ContentDialog
        {
            XamlRoot = AppWindowsLauncherService.GetXamlRootForWinType(typeof(RecordsListPage)),
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete this file \"{record.FileName}\"?",
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel"
        };

        ContentDialogResult result = await renameDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var file = await FileAccessService.GetFileFromVideoFolderByNameAsync(record.FileName);
            file.DeleteAsync();
            Records.Remove(record);
            
            NotificationService.ShowToastNotification($"Record {record.FileName} has been successfully deleted");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}