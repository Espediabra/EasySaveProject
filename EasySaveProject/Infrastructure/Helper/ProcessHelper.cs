using System.Diagnostics;

namespace EasySaveProject.Infrastructure.Process;

public static class ProcessHelper
{
    public static int Run(string exePath, string arguments)
    {
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Executable introuvable : {exePath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        string normalized = processName.Trim().ToLowerInvariant().Replace(".exe", "");
        return System.Diagnostics.Process.GetProcessesByName(normalized).Length > 0;
    }
}
