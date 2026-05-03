using System;

namespace EasyLog
{
    // Représente une entrée de log : toutes les informations d'une opération de sauvegarde
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string JobName { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string TargetPath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public long TransferTimeMs { get; set; }

        public LogLevel Level { get; set; } = LogLevel.INFO;

        public string Message { get; set; } = string.Empty;
    }
}
