using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyScreenRecorder.Services;

internal static class FileAccessService
{
    internal static async Task<StorageFile> GetFileFromVideoFolderByNameAsync(string fileName)
    {
        var storageFolder = await KnownFolders.VideosLibrary.GetFolderAsync(nameof(MyScreenRecorder));
        return await storageFolder.GetFileAsync(fileName);
    }

    internal static async Task<StorageFile> GetFileFromLocalStorageByNameAsync(string fileName)
    {
        return await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
    }
    
    internal static async Task<string> GetPathFromVideoFolderByNameAsync(string fileName)
    {
        var storageFolder = await KnownFolders.VideosLibrary.GetFolderAsync(nameof(MyScreenRecorder));
        return Path.Combine(storageFolder.Path, fileName);
    }

    internal static string GetPathFromLocalStorageByName(string fileName)
    {
        return Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
    }
}