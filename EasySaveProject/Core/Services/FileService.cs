namespace EasySaveProject.Core.Services;

public class FileService
{
    private const int ChunkSize = 1024 * 1024; // 1 Mo par chunk

    public void CopyFile(string source, string target)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException($"Source file not found: {source}");

        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.Copy(source, target, true);
    }

    public void CopyFileWithProgress(string source, string target, Action<long>? onProgress = null)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException($"Source file not found: {source}");

        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var sourceStream = new FileStream(
            source,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            ChunkSize,
            FileOptions.SequentialScan);

        using var targetStream = new FileStream(
            target,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            ChunkSize,
            FileOptions.SequentialScan);

        var buffer = new byte[ChunkSize];
        int bytesRead;

        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            targetStream.Write(buffer, 0, bytesRead);
            onProgress?.Invoke(bytesRead);
        }
    }
}