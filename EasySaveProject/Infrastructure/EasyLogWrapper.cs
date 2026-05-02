using System;
using EasyLog;

namespace EasySave.Infrastructure
{
    // Rôle de l'adaptateur, il fait le lien entre notre application EasySave et la DLL externe EasyLog
    public class EasyLogWrapper
    {
        // Le fournisseur de logs (ici JSON) venant de la DLL EasyLog
        private readonly ILogProvider _provider;

        // Le fournisseur de logs à utiliser
        public EasyLogWrapper(ILogProvider provider)
        {
            _provider = provider;
        }

        // Méthodes publiques

        // Enregistre les logs des différents nivezux Info, Warn et Error
        public void LogInfo(string jobName, string sourcePath, string targetPath,
                            long fileSizeBytes, long transferTimeMs, string message)
        {
            WriteLog(LogLevel.INFO, jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);
        }

        public void LogWarning(string jobName, string sourcePath, string targetPath,
                               long fileSizeBytes, long transferTimeMs, string message)
        {
            WriteLog(LogLevel.WARNING, jobName, sourcePath, targetPath, fileSizeBytes, transferTimeMs, message);
        }

        public void LogError(string jobName, string sourcePath, string targetPath,
                             long fileSizeBytes, string message)
        {
            WriteLog(LogLevel.ERROR, jobName, sourcePath, targetPath, fileSizeBytes, -1, message);
        }

        // Méthode privée

        private void WriteLog(LogLevel level, string jobName, string sourcePath,
                              string targetPath, long fileSizeBytes, long transferTimeMs, string message)
        {
            try
            {
                // Construction de l'entrée de log
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    FileSizeBytes = fileSizeBytes,
                    TransferTimeMs = transferTimeMs,
                    Level = level,
                    Message = message
                };

                _provider.Write(entry);
            }
            catch (Exception ex)
            {
                // Exception d'erreur (je parle fr wsh)
                Console.Error.WriteLine($"[EasyLogWrapper] Échec de l'écriture du log : {ex.Message}");
            }
        }
    }
}
