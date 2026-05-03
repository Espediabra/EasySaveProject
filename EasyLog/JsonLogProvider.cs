using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyLog
{
    /// Implémentation de ILogProvider qui stocke les logs au format JSON.
    public class JsonLogProvider : ILogProvider
    {
        // Dossier où les fichiers de log seront créés
        private readonly string _logDirectory;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Verrou pour éviter les conflits
        private readonly object _lock = new object();

        /// Constructeur : on lui donne le dossier où écrire les logs.Si dossier n'existe pas, on le crée automatiquement + Chemin absolu vers le dossier de logs
        public JsonLogProvider(string logDirectory)
        {
            _logDirectory = logDirectory;

            // On s'assure que le dossier existe avant toute écriture
            Directory.CreateDirectory(_logDirectory);
        }

        /// Écrit une entrée de log dans le fichier JSON du jour. Utilisation de Strategy ?

        public void Write(LogEntry entry)
        {
            // Un seul thread à la fois Mutex (Exclusion mutuelle)
            lock (_lock)
            {
                string filePath = GetFilePath(entry.Timestamp);

                List<LogEntry> entries = ReadFromFile(filePath);

                entries.Add(entry);

                string json = JsonSerializer.Serialize(entries, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
        }

        /// Retourne toutes les entrées de log d'un jour donné
        public List<LogEntry> ReadByDate(DateTime date)
        {
            string filePath = GetFilePath(date);
            return ReadFromFile(filePath);
        }

        // Méthodes privées

        private string GetFilePath(DateTime date)
        {
            string fileName = date.ToString("yyyy-MM-dd") + ".json";
            return Path.Combine(_logDirectory, fileName);
        }

        private List<LogEntry> ReadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<LogEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOptions)
                       ?? new List<LogEntry>();
            }
            catch
            {
                return new List<LogEntry>();
            }
        }
    }
}
