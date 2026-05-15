using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog;
using EasySaveProject.Infrastructure;
using EasySaveProject.Infrastructure.Logging;
using EasySaveProject.Core.Localization;

namespace EasySaveProject.Core.Services;

public class LogService
{
    private readonly LocalizationService _loc;

    private static LogService? _instance;
    private static readonly object _instanceLock = new object();

    // AppConfig optionnel, Sans AppConfig on est en mode Localidentique à la 2.0
    public static void Initialize(
        LocalizationService loc,
        LogFormat? format = null,
        AppConfig? config = null)
    {
        lock (_instanceLock)
        {
            _instance = new LogService(loc, format, config);
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

    private LogService(LocalizationService loc, LogFormat? format, AppConfig? config)
    {
        _loc = loc;

        string logDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "Data", "Logs"));
        Directory.CreateDirectory(logDir);

        if (format == null)
        {
            string env = Environment.GetEnvironmentVariable("EASYSAVE_LOG_FORMAT") ?? "json";
            format = env.ToLower() == "xml" ? LogFormat.Xml : LogFormat.Json;
        }

        ILogProvider localProvider = format == LogFormat.Xml
            ? new XmlLogProvider(logDir)
            : new JsonLogProvider(logDir);

        _provider = BuildProvider(localProvider, config);

        _wrapper = new EasyLogWrapper(_provider);
    }

    private static ILogProvider BuildProvider(ILogProvider local, AppConfig? config)
    {
        if (config == null || config.LogMode == LogMode.Local)
            return local;

        var remote = new RemoteLogProvider(
            config.LogServerHost,
            config.LogServerPort,
            config.MachineId
        );

        return config.LogMode switch
        {
            LogMode.Remote => remote,
            LogMode.Both => new CompositeLogProvider(local, remote),
            _ => local
        };
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
}
