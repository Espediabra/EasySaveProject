using EasySaveProject.Models;
using EasySaveProject.Services;
using EasySaveProject.Factories;

namespace EasySaveProject.Services
{
    public class BackupService
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly StateService _stateService;

        private readonly List<BackupJob> _jobs = new();

        public BackupService(
            FileService fileService,
            LogService logService,
            StateService stateService)
        {
            _fileService = fileService;
            _logService = logService;
            _stateService = stateService;
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
        }

        public void AddJob(BackupJob job)
        {
            if (_jobs.Count >= 5)
                throw new InvalidOperationException("Maximum number of jobs reached");

            _jobs.Add(job);
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
        }

        public IReadOnlyList<BackupJob> GetJobs()
        {
            return _jobs;
        }
    }
}