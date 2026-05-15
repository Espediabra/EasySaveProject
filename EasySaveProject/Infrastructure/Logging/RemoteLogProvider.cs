using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyLog;

namespace EasySaveProject.Infrastructure.Logging;

public class RemoteLogProvider : ILogProvider
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _machineId;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public RemoteLogProvider(string host, int port, string machineId)
    {
        _host = host;
        _port = port;
        _machineId = machineId;
    }

    public void Write(LogEntry entry)
    {
        _ = Task.Run(() => SendAsync(entry));
    }

    public List<LogEntry> ReadByDate(DateTime date)
    {
        return new List<LogEntry>();
    }

    private async Task SendAsync(LogEntry entry)
    {
        try
        {
            var envelope = new RemoteLogEnvelope
            {
                MachineId = _machineId,
                Entry = entry
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(_host, _port);
            if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                return; 

            if (!client.Connected) return;

            var stream = client.GetStream();
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
        }
        catch
        {
        }
    }
}

public class RemoteLogEnvelope
{
    public string MachineId { get; set; } = string.Empty;
    public LogEntry Entry { get; set; } = new();
}
