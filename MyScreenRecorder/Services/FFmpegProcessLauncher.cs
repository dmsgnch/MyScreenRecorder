using System.Diagnostics;

namespace MyScreenRecorder.Services;

internal class FFmpegProcessLauncher
{
    private string FFmpegFilePath { get; }
    
    internal FFmpegProcessLauncher(string ffmpegFilePath)
    {
        FFmpegFilePath = ffmpegFilePath;
    }
    
    internal void StartSimpleProcess(string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = FFmpegFilePath,
            Arguments = arguments,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();
            process.WaitForExit();
        }
    }
}