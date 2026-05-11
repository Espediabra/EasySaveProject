using System;
using EasyLog;

namespace EasySaveProject.Infrastructure
{
    public class EasyLogWrapper
    {
        private readonly ILogProvider _provider;

        public EasyLogWrapper(ILogProvider provider)
        {
            _provider = provider;
        }

        public void LogInfo(
            string jobName,
            string sourcePath,
            string targetPath,
            long fileSizeBytes,
            long transferTimeMs,
            string message)
        {
            WriteLog(
                LogLevel.INFO,
                jobName,
                sourcePath,
                targetPath,
                fileSizeBytes,
                transferTimeMs,
                message
            );
        }

        public void LogWarning(
            string jobName,
            string sourcePath,
            string targetPath,
            long fileSizeBytes,
            long transferTimeMs,
            string message)
        {
            WriteLog(
                LogLevel.WARNING,
                jobName,
                sourcePath,
                targetPath,
                fileSizeBytes,
                transferTimeMs,
                message
            );
        }

        public void LogError(
            string jobName,
            string sourcePath,
            string targetPath,
            long fileSizeBytes,
            string message)
        {
            WriteLog(
                LogLevel.ERROR,
                jobName,
                sourcePath,
                targetPath,
                fileSizeBytes,
                -1,
                message
            );
        }

        private void WriteLog(
            LogLevel level,
            string jobName,
            string sourcePath,
            string targetPath,
            long fileSizeBytes,
            long transferTimeMs,
            string message)
        {
            try
            {
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

                Console.WriteLine(
                    $"[{entry.Timestamp:HH:mm:ss}] " +
                    $"[{entry.Level}] " +
                    $"{entry.JobName} - {entry.Message}"
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[EasyLogWrapper] Échec de l'écriture du log : {ex.Message}"
                );
            }
        }
    }
}