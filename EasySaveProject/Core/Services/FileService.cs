namespace EasySaveProject.Services;

public class FileService
{
    public void CopyFile(string source, string target)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException($"Source file not found: {source}");

        var directory = Path.GetDirectoryName(target);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.Copy(source, target, true);
    }
}