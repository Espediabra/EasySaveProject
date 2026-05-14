public class LogDto
{
    public DateTime Time { get; set; }
    public string Level { get; set; }
    public string Job { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSizeBytes { get; set; }
    public long TransferTimeMs { get; set; }
    public string Message { get; set; }
}