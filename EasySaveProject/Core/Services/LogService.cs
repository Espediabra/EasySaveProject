using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog;
using EasySaveProject.Infrastructure;

namespace EasySaveProject.Services;


// Service principal de logging + Pattern Singleton
public class LogService
{
    // Singleton

    private static LogService? _instance;

    // Verrou 
    private static readonly object _instanceLock = new object();

    /// Point d'accès
    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                        _instance = new LogService();
                }
            }
            return _instance;
        }
    }

    // Dépendances

    private readonly EasyLogWrapper _wrapper;

    private readonly ILogProvider _provider;

    // Constructeur privé

    private LogService()
    {
        // Lecture du chemin de logs 
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
        Console.WriteLine($"{"Heure",-10} {"Niveau",-10} {"Job",-25} {"Message"}");
        Console.WriteLine(new string('-', 70));

        foreach (var e in entries)
        {
            Console.WriteLine(
                $"{e.Timestamp:HH:mm:ss} " +
                $"{e.Level,-10} " +
                $"{e.JobName,-20} " +
                $"{e.Message,-30} " +
                $"...{(e.SourcePath.Substring(Math.Max(0, e.SourcePath.Length - 17))),-20} ➟ " +
                $"...{(e.TargetPath.Substring(Math.Max(0, e.TargetPath.Length - 17))), -20} " +
                $"{e.FileSizeBytes,12} octets " +
                $"{(e.TransferTimeMs < 0 ? "ERREUR" : $"{e.TransferTimeMs} ms"),10}"
            );
        }
    }

    /// Vue de kla console
    public void PrintDetailed(List<LogEntry> entries)
    {
        foreach (var e in entries)
        {
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  Timestamp      : {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Niveau         : {e.Level}");
            Console.WriteLine($"  Job            : {e.JobName}");
            Console.WriteLine($"  Source         : {e.SourcePath}");
            Console.WriteLine($"  Destination    : {e.TargetPath}");
            Console.WriteLine($"  Taille         : {e.FileSizeBytes} octets");
            Console.WriteLine($"  Temps transfert: {e.TransferTimeMs} ms" +
                              (e.TransferTimeMs < 0 ? "ERREUR" : ""));
            Console.WriteLine($"  Message        : {e.Message}");
        }
        Console.WriteLine(new string('═', 60));
    }
}

