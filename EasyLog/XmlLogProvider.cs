using System.Xml.Linq;

namespace EasyLog;

// La même interface que JsonLogProvider (ILogProvider) seul le stockage change
public class XmlLogProvider : ILogProvider
{
    private readonly string _logDirectory;
    private readonly object _lock = new object();

    // Mettre le dossier où écrire les fichiers XML.
    public XmlLogProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            string path = GetFilePath(entry.Timestamp);
            XDocument document = ReadOrCreateDocument(path);

            document.Root!.Add(EntryToXml(entry));

            document.Save(path);
        }
    }

    public List<LogEntry> ReadByDate(DateTime date)
    {
        string path = GetFilePath(date);

        if (!File.Exists(path))
            return new List<LogEntry>();

        try
        {
            var document = XDocument.Load(path);

            return document.Root!
                .Elements("Entry")
                .Select(XmlToEntry)
                .ToList();
        }
        catch
        {
            return new List<LogEntry>();
        }
    }

    private string GetFilePath(DateTime date)
        => Path.Combine(_logDirectory, $"{date:yyyy-MM-dd}.xml");

    private XDocument ReadOrCreateDocument(string path)
    {
        if (File.Exists(path))
        {
            try { return XDocument.Load(path); }
            catch {}
        }

        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("Logs")
        );
    }

    private XElement EntryToXml(LogEntry entry)
        => new XElement("Entry",
            new XElement("Timestamp", entry.Timestamp.ToString("o")),
            new XElement("JobName", entry.JobName),
            new XElement("SourcePath", entry.SourcePath),
            new XElement("TargetPath", entry.TargetPath),
            new XElement("FileSizeBytes", entry.FileSizeBytes),
            new XElement("TransferTimeMs", entry.TransferTimeMs),
            new XElement("Level", entry.Level.ToString()),
            new XElement("Message", entry.Message)
        );

    private LogEntry XmlToEntry(XElement element)
        => new LogEntry
        {
            Timestamp = DateTime.Parse(element.Element("Timestamp")!.Value),
            JobName = element.Element("JobName")!.Value,
            SourcePath = element.Element("SourcePath")!.Value,
            TargetPath = element.Element("TargetPath")!.Value,
            FileSizeBytes = long.Parse(element.Element("FileSizeBytes")!.Value),
            TransferTimeMs = long.Parse(element.Element("TransferTimeMs")!.Value),
            Level = Enum.Parse<LogLevel>(element.Element("Level")!.Value),
            Message = element.Element("Message")!.Value
        };
}
