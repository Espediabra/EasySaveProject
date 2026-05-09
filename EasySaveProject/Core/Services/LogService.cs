using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog;
using EasySaveProject.Infrastructure;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.Core.Services;


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
        "Data", "Logs"));

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
}

