using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog;
using EasySaveProject.Infrastructure;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.Services;

public class LogService
{
    private readonly LocalizationService _loc;

    private static LogService? _instance;
    private static readonly object _instanceLock = new object();

    public static void Initialize(LocalizationService loc, LogFormat? format = null)
    {
        lock (_instanceLock)
        {
            _instance = new LogService(loc, format);
        }
    }

    public static LogService Instance
    {
        get
        {
            if (_instance == null)
                throw new InvalidOperationException("LogService not initialized.");
            return _instance;
        }
    }

    private readonly EasyLogWrapper _wrapper;
    private readonly ILogProvider _provider;

    private LogService(LocalizationService loc, LogFormat? format)
    {
        _loc = loc;

        string logDir = Environment.GetEnvironmentVariable("EASYSAVE_LOG_DIR")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "Data", "Logs"));

        Directory.CreateDirectory(logDir);

        if (format == null)
        {
            string env = Environment.GetEnvironmentVariable("EASYSAVE_LOG_FORMAT") ?? "json";
            format = env.ToLower() == "xml" ? LogFormat.Xml : LogFormat.Json;
        }

        _provider = format == LogFormat.Xml
            ? new XmlLogProvider(logDir)
            : new JsonLogProvider(logDir);

        _wrapper = new EasyLogWrapper(_provider);
    }

    public void LogInfo(string jobName, string sourcePath, string targetPath,
                        long fileSizeBytes, long transferTimeMs, string message)
        => _wrapper.LogInfo(jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);

    public void LogWarning(string jobName, string sourcePath, string targetPath,
                           long fileSizeBytes, long transferTimeMs, string message)
        => _wrapper.LogWarning(jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);

    public void LogError(string jobName, string sourcePath, string targetPath,
                         long fileSizeBytes, string message)
        => _wrapper.LogError(jobName, sourcePath, targetPath, fileSizeBytes, message);

    public List<LogEntry> GetByDate(DateTime date)
        => _provider.ReadByDate(date);

    public List<LogEntry> GetByLevel(DateTime date, LogLevel level)
        => GetByDate(date).Where(e => e.Level == level).ToList();

    public List<LogEntry> GetByJobName(DateTime date, string jobName)
        => GetByDate(date)
           .Where(e => e.JobName.Equals(jobName, StringComparison.OrdinalIgnoreCase))
           .ToList();

    public void PrintSimple(List<LogEntry> entries)
    {
        Console.WriteLine(
            $"{_loc.T("Logs.Header.Time"),-10} " +
            $"{_loc.T("Logs.Header.Level"),-10} " +
            $"{_loc.T("Logs.Header.Job"),-25} " +
            $"{_loc.T("Logs.Header.Message"),-30} " +
            $"{_loc.T("Logs.Header.SourceFile"),-17} " +
            $"{_loc.T(" "),-7} " +
            $"{_loc.T("Logs.Header.TargetFile"),-17} " +
            $"{_loc.T("Logs.Header.FileSize"),-12} " +
            $"{_loc.T("Logs.Header.TransferTime"),-10}"
        );

        Console.WriteLine(new string('-', 152));

        foreach (var e in entries)
        {
            string source = string.IsNullOrEmpty(e.SourcePath)
                ? ""
                : "..." + e.SourcePath.Substring(Math.Max(0, e.SourcePath.Length - 17));

            string target = string.IsNullOrEmpty(e.TargetPath)
                ? ""
                : "..." + e.TargetPath.Substring(Math.Max(0, e.TargetPath.Length - 17));

            Console.WriteLine(
                $"{e.Timestamp:HH:mm:ss} " +
                $"{_loc.T($"Log.Level.{e.Level}"),-10} " +
                $"{e.JobName,-20} " +
                $"{e.Message,-30} " +
                $"{source}" +
                $"{_loc.T("Logs.Arrow"),-7}" +
                $"{target}" +
                $"{e.FileSizeBytes,12} {_loc.T("Logs.Bytes")} " +
                $"{(e.TransferTimeMs < 0
                    ? _loc.T("Logs.Error")
                    : $"{e.TransferTimeMs} {_loc.T("Logs.Milliseconds")}"),10}"
            );
        }
    }

    public void PrintDetailed(List<LogEntry> entries)
    {
        foreach (var e in entries)
        {
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  {_loc.T("Logs.Details.Timestamp"),-15}: {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Level"),-15}: {_loc.T($"Log.Level.{e.Level}")}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Job"),-15}: {e.JobName}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Source"),-15}: {e.SourcePath}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Destination"),-15}: {e.TargetPath}");
            Console.WriteLine($"  {_loc.T("Logs.Details.Size"),-15}: {e.FileSizeBytes} {_loc.T("Logs.Bytes")}");
            Console.WriteLine(
                $"  {_loc.T("Logs.Details.TransferTime"),-15}: " +
                $"{(e.TransferTimeMs < 0
                    ? _loc.T("Logs.Error")
                    : $"{e.TransferTimeMs} {_loc.T("Logs.Milliseconds")}")}"
            );
            Console.WriteLine($"  {_loc.T("Logs.Details.Message"),-15}: {e.Message}");
        }

        Console.WriteLine(new string('═', 60));
    }
}