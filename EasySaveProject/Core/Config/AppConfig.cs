using System.Text.Json;
using System.Text.Json.Serialization;
using EasyLog;

public class AppConfig
{
    public string Langage { get; set; } = "en";
    public bool FirstRun { get; set; } = true;
    public LogFormat LogFormat { get; set; } = LogFormat.Json;

    public string CryptoKey { get; set; } = string.Empty;
    public List<string> CryptoExtensions { get; set; } = new();

    [JsonConverter(typeof(StringOrListConverter))]
    public List<string> BusinessSoftware { get; set; } = new();

    // V3: extensions processed before all other files across parallel jobs (e.g. [".zip", ".iso"])
    public List<string> PriorityExtensions { get; set; } = new();

    // V3: max file size (KB) allowed to transfer simultaneously. 0 = disabled.
    public long LargeFileThresholdKb { get; set; } = 0;

    // V3: log centralization mode (Local / Remote / Both)
    public LogMode LogMode { get; set; } = LogMode.Local;
    public string LogServerHost { get; set; } = "localhost";
    public int LogServerPort { get; set; } = 9000;
    public string MachineId { get; set; } = Environment.MachineName;
}

public enum LogMode { Local, Remote, Both }

public class StringOrListConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return string.IsNullOrWhiteSpace(s) ? new List<string>() : new List<string> { s };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var item = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(item))
                        list.Add(item);
                }
            }
            return list;
        }

        if (reader.TokenType == JsonTokenType.Null)
            return new List<string>();

        return new List<string>();
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var s in value)
            writer.WriteStringValue(s);
        writer.WriteEndArray();
    }
}