using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace RecordingRuntimeComponent
{
    public sealed class RecordingBackgroundTask : IBackgroundTask
    {
        volatile bool cancelRequested = false;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            
            var cost = BackgroundWorkCost.CurrentBackgroundWorkCost;
            if (cost == BackgroundWorkCostValue.High)
                return;

            var cancel = new CancellationTokenSource();
            taskInstance.Canceled += (s, e) =>
            {
                cancel.Cancel();
                cancel.Dispose();
                cancelRequested = true;
            };

            try
            {
                await RecordScreenAsync(taskInstance);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async Task RecordScreenAsync(IBackgroundTaskInstance taskInstance)
        {
            var settings = ApplicationData.Current.LocalSettings;
            string arguments = (string)settings.Values["arguments"];
            string outputFileName = (string)settings.Values["outputFileName"];

            string ffmpegFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ffmpeg.exe");            
            string outputFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, outputFileName);          

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegFilePath,
                Arguments = arguments + " " + outputFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                
                var outputTask = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string outputLine = await process.StandardOutput.ReadLineAsync();
                        Debug.WriteLine($"FFmpeg output: {outputLine}");
                    }
                });

                var errorTask = Task.Run(async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        string errorLine = await process.StandardError.ReadLineAsync();
                        Debug.WriteLine($"FFmpeg error: {errorLine}");
                    }
                });

                await Task.Run(async () =>
                {
                    while (!process.HasExited)
                    {
                        if (cancelRequested)
                        {
                            process.StandardInput.WriteLine("q");
                            break;
                        }

                        await Task.Delay(100);
                    }
                });

                await Task.WhenAll(outputTask, errorTask);
    
                process.WaitForExit();
            }
        }
    }
}