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

        string normalized = processName.Trim().ToLowerInvariant();
        if (normalized.EndsWith(".exe"))
            normalized = normalized.Substring(0, normalized.Length - 4);

        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (System.Diagnostics.Process.GetProcessesByName(normalized).Length > 0)
            return true;

        var all = System.Diagnostics.Process.GetProcesses();
        foreach (var p in all)
        {
            try
            {
                if (p.ProcessName.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    DisposeAll(all);
                    return true;
                }
            }
            catch
            {
            }
        }

        DisposeAll(all);
        return false;
    }

    private static void DisposeAll(System.Diagnostics.Process[] processes)
    {
        foreach (var p in processes)
        {
            try { p.Dispose(); } catch { }
        }
    }
}