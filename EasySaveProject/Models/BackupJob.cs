namespace EasySave.Models
{
    public class BackupJob
    {
        public string Name { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }
        public BackupType Type { get; }

        public BackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            Name = name;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            Type = type;
        }
    }
}