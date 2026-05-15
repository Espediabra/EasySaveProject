using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Serveur TCP de centralisation des logs EasySave conçu pour Docker

class LogServer
{
    private static readonly int    Port   = int.Parse(Environment.GetEnvironmentVariable("LOG_SERVER_PORT") ?? "9000");
    private static readonly string LogDir = Environment.GetEnvironmentVariable("LOG_DIR") ?? "/logs";

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object>
        _fileLocks = new();

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    static async Task Main()
    {
        Directory.CreateDirectory(LogDir);

        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        Console.WriteLine($"[LogServer] Listening on port {Port}");
        Console.WriteLine($"[LogServer] Writing logs to {LogDir}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[LogServer] Client connected: {remote}");

        try
        {
            using var stream = client.GetStream();
            var buffer  = new StringBuilder();
            var readBuf = new byte[4096];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(readBuf)) > 0)
            {
                buffer.Append(Encoding.UTF8.GetString(readBuf, 0, bytesRead));

                var content = buffer.ToString();
                var lines   = content.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    var line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                        ProcessLine(line);
                }

                buffer.Clear();
                buffer.Append(lines[^1]);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LogServer] Client {remote} error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
            Console.WriteLine($"[LogServer] Client disconnected: {remote}");
        }
    }

    private static void ProcessLine(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<LogEnvelope>(json, _options);
            if (envelope?.Entry == null) return;

            var date     = envelope.Entry.Timestamp.ToString("yyyy-MM-dd");
            var machine  = SanitizeFilename(envelope.MachineId);
            var filePath = Path.Combine(LogDir, $"{date}_{machine}.json");

            var fileLock = _fileLocks.GetOrAdd(filePath, _ => new object());

            lock (fileLock)
            {
                var entries = ReadExisting(filePath);
                entries.Add(envelope.Entry);
                File.WriteAllText(filePath, JsonSerializer.Serialize(entries, _options));
            }

            Console.WriteLine($"[LogServer] [{envelope.MachineId}] {envelope.Entry.Level} - {envelope.Entry.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LogServer] Failed to process entry: {ex.Message}");
        }
    }

    private static List<LogEntry> ReadExisting(string filePath)
    {
        if (!File.Exists(filePath)) return new List<LogEntry>();

        try
        {
            var json   = File.ReadAllText(filePath);
            var parsed = JsonSerializer.Deserialize<List<LogEntry>>(json, _options);
            return parsed ?? new List<LogEntry>();
        }
        catch
        {
            return new List<LogEntry>();
        }
    }

    private static string SanitizeFilename(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "unknown" : name;
    }
}

public class LogEnvelope
{
    public string   MachineId { get; set; } = string.Empty;
    public LogEntry Entry     { get; set; } = new();
}

public class LogEntry
{
    public DateTime Timestamp      { get; set; }
    public string   JobName        { get; set; } = string.Empty;
    public string   SourcePath     { get; set; } = string.Empty;
    public string   TargetPath     { get; set; } = string.Empty;
    public long     FileSizeBytes  { get; set; }
    public long     TransferTimeMs { get; set; }
    public LogLevel Level          { get; set; }
    public string   Message        { get; set; } = string.Empty;
}

public enum LogLevel { INFO, WARNING, ERROR }
