using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog;
using EasySaveProject.Infrastructure;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.Services;


// Service principal de logging + Pattern Singleton
public class LogService
{

    // Dépendances
    private readonly LocalizationService _loc;

    // Singleton
    private static LogService? _instance;
    // Verrou 
    private static readonly object _instanceLock = new object();

    public static void Initialize(LocalizationService loc)
    {
        if (_instance == null)
        {
            lock (_instanceLock)
            {
                if (_instance == null)
                    _instance = new LogService(loc);
            }
        }
    }

    /// Point d'accès
    public static LogService Instance
    {
        get
        {
            if (_instance == null)
                throw new Exception("LogService not initialized. Call Initialize() first.");

            return _instance;
        }
    }

    // Dépendances

    private readonly EasyLogWrapper _wrapper;

    private readonly ILogProvider _provider;

    // Constructeur privé

    private LogService(LocalizationService loc)
    {
        _loc = loc;

        string logDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "Data", "Logs"));

        _provider = new JsonLogProvider(logDir);
        _wrapper = new EasyLogWrapper(_provider);
    }

    // Méthodes d'écriture pour les 3 niveaux EKIP

    public void LogInfo(string jobName, string sourcePath, string targetPath,
                        long fileSizeBytes, long transferTimeMs, string message)
        => _wrapper.LogInfo(jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);

    public void LogWarning(string jobName, string sourcePath, string targetPath,
                           long fileSizeBytes, long transferTimeMs, string message)
        => _wrapper.LogWarning(jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);

    public void LogError(string jobName, string sourcePath, string targetPath,
                         long fileSizeBytes, string message)
        => _wrapper.LogError(jobName, sourcePath, targetPath, fileSizeBytes, message);

    // Méthodes de lecture et filtrage

    public List<LogEntry> GetByDate(DateTime date)
        => _provider.ReadByDate(date);

    public List<LogEntry> GetByLevel(DateTime date, LogLevel level)
        => GetByDate(date)
           .Where(e => e.Level == level)
           .ToList();

    public List<LogEntry> GetByJobName(DateTime date, string jobName)
        => GetByDate(date)
           .Where(e => e.JobName.Equals(jobName, StringComparison.OrdinalIgnoreCase))
           .ToList();

    // Méthodes d'affichage

    /// Affiche une vue simplifiée d'une liste de logs dans la console.
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
            Console.WriteLine(
                $"{e.Timestamp:HH:mm:ss} " +
                $"{_loc.T($"{e.Level}"),-10} " +
                $"{e.JobName,-20} " +
                $"{e.Message,-30} " +
                $"...{e.SourcePath.Substring(Math.Max(0, e.SourcePath.Length - 17))}" +
                $"{_loc.T("Logs.Arrow"),-7}" +
                $"...{e.TargetPath.Substring(Math.Max(0, e.TargetPath.Length - 17))}" +
                $"{e.FileSizeBytes,12} {_loc.T("Logs.Bytes")} " +
                $"{(e.TransferTimeMs < 0
                    ? _loc.T("Logs.Error")
                    : $"{e.TransferTimeMs} {_loc.T("Logs.Milliseconds")}"),10}"
            );
        }
    }

    /// Vue de la console
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

