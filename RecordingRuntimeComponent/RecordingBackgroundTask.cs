using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace RecordingRuntimeComponent;

public sealed class RecordingBackgroundTask : IBackgroundTask
{
    private const string StopRecordingFFmpegCommand = "q";
    volatile bool cancelRequested = false;

    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();

        var cost = BackgroundWorkCost.CurrentBackgroundWorkCost;
        if (cost == BackgroundWorkCostValue.High)
            return;
        
        taskInstance.Canceled += (s, e) =>
        {
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
        var startInfo = CreateProcessStartInfoUsingAppDataContainer();

        await StartFFmpegProcessAsync(startInfo);
    }

    private ProcessStartInfo CreateProcessStartInfoUsingAppDataContainer()
    {
        var settings = ApplicationData.Current.LocalSettings;
        string arguments = (string)settings.Values["arguments"];
        string outputFileName = (string)settings.Values["outputFileName"];

        string ffmpegFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ffmpeg.exe");
        string outputFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, outputFileName);

        return new ProcessStartInfo
        {
            FileName = ffmpegFilePath,
            Arguments = arguments + " " + outputFilePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private async Task StartFFmpegProcessAsync(ProcessStartInfo startInfo)
    {
        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();

            var outputTask = Task.Run(async () => { await StandardOutputReading(process); });
            var errorTask = Task.Run(async () => { await ErrorOutputReading(process); });

            await Task.Run(async () =>
            {
                while (!process.HasExited)
                {
                    if (cancelRequested)
                    {
                        await WriteStopCommandAsync(process);
                        break;
                    }

                    await Task.Delay(100);
                }
            });

            await Task.WhenAll(outputTask, errorTask);

            process.WaitForExit();
        }
    }

    private async Task StandardOutputReading(Process process)
    {
        while (!process.StandardOutput.EndOfStream)
        {
            string outputLine = await process.StandardOutput.ReadLineAsync();
            Debug.WriteLine($"FFmpeg output: {outputLine}");
        }
    }

    private async Task ErrorOutputReading(Process process)
    {
        await Task.Run(async () =>
        {
            using (var client = new NamedPipeClientStream(".", @"LOCAL\MYPIPE", PipeDirection.Out))
            {
                await client.ConnectAsync();

                using (var writer = new StreamWriter(client) { AutoFlush = true })
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        string errorLine = await process.StandardError.ReadLineAsync();

                        await writer.WriteLineAsync(errorLine);
                        Debug.WriteLine($"FFmpeg error: {errorLine}");
                    }
                }
            }
        });
    }

    private async Task WriteStopCommandAsync(Process process)
    {
        await process.StandardInput.WriteLineAsync(StopRecordingFFmpegCommand);
    }
}