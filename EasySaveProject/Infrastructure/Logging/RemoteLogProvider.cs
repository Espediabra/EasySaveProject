using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyLog;

namespace EasySaveProject.Infrastructure.Logging;

// Envoie chaque LogEntry vers un serveur TCP distant DOcker
public class RemoteLogProvider : ILogProvider
{
    private readonly string _host;
    private readonly int    _port;
    private readonly string _machineId;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        Converters    = { new JsonStringEnumConverter() }
    };

    public RemoteLogProvider(string host, int port, string machineId)
    {
        _host      = host;
        _port      = port;
        _machineId = machineId;
    }

    public void Write(LogEntry entry)
    {
        try
        {
            var envelope = new RemoteLogEnvelope
            {
                MachineId = _machineId,
                Entry     = entry
            };

            var json  = JsonSerializer.Serialize(envelope, _options);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            using var client = new TcpClient();
            client.ConnectAsync(_host, _port).Wait(TimeSpan.FromSeconds(2));

            if (!client.Connected) return;

            using var stream = client.GetStream();
            stream.Write(bytes, 0, bytes.Length);
        }
        catch
        {
        }
    }

    public List<LogEntry> ReadByDate(DateTime date)
    {
        return new List<LogEntry>();
    }
}

public class RemoteLogEnvelope
{
    public string   MachineId { get; set; } = string.Empty;
    public LogEntry Entry     { get; set; } = new();
}
