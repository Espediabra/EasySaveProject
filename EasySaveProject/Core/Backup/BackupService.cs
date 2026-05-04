using EasySaveProject.Models;
using EasySaveProject.Services;
using EasySaveProject.Factories;
using System.Text.Json;

namespace EasySaveProject.Services
{
    public class BackupService
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly StateService _stateService;

        private readonly List<BackupJob> _jobs = new();

        private readonly string _jobsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "Data", "Jobs", "jobs.json"
        );

        public BackupService(FileService fileService, LogService logService, StateService stateService)
        {
            _fileService = fileService;
            _logService = logService;
            _stateService = stateService;

            if (File.Exists(_jobsPath))
            {
                var json = File.ReadAllText(_jobsPath);

                var jobs = System.Text.Json.JsonSerializer.Deserialize<List<BackupJob>>(json);

                if (jobs != null)
                {
                    _jobs.AddRange(jobs);
                }
            }
        }

        public void LoadJobs(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var jobs = System.Text.Json.JsonSerializer.Deserialize<List<BackupJob>>(json);

            _jobs.Clear();

            if (jobs == null)
                throw new Exception("Invalid jobs configuration file");

            _jobs.AddRange(jobs);
        }


        public void UpdateJobType(int index, BackupType type)
        {
            if (index < 0 || index >= _jobs.Count)
                return;

            var oldJob = _jobs[index];

            var updatedJob = new BackupJob(
                oldJob.Name,
                oldJob.SourcePath,
                oldJob.TargetPath,
                type
            );

            _jobs[index] = updatedJob;

            SaveJobs();
        }

        public void AddJob(BackupJob job)
        {
            if (_jobs.Count >= 5)
                throw new InvalidOperationException("Maximum number of jobs reached");

            _jobs.Add(job);

            SaveJobs();
        }

        public void RunJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                return;

            var job = _jobs[index];

            var strategy = BackupStrategyFactory.Create(job.Type);

            strategy.Execute(job, _fileService, _logService, _stateService);
        }

        public void DeleteJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                return;

            _jobs.RemoveAt(index);

            SaveJobs();
        }

        private void SaveJobs()
        {
            var directory = Path.GetDirectoryName(_jobsPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(_jobs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_jobsPath, json);
        }

        public IReadOnlyList<BackupJob> GetJobs()
        {
            return _jobs;
        }
    }
}