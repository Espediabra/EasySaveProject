using EasySave.Models;
using EasySave.Services;
using EasySave.Factories;

namespace EasySave.Services
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

        public void AddJob(BackupJob job)
        {
            if (_jobs.Count >= 5)
                throw new InvalidOperationException("Maximum number of jobs reached");

            _jobs.Add(job);
        }

        public void RunJobs(List<int> ids)
        {
            foreach (var id in ids)
            {
                if (id < 1 || id > _jobs.Count)
                    continue;

                var job = _jobs[id - 1];

                var strategy = BackupStrategyFactory.Create(job.Type);

                strategy.Execute(job, _fileService, _logService, _stateService);
            }
        }
    }
}