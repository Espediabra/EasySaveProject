using EasySaveProject.Models;
using EasySaveProject.Core.Services;
using EasySaveProject.Factories;
using EasySaveProject.Infrastructure.Crypto;
using EasySaveProject.Infrastructure.Monitoring;
using System.Collections.Concurrent;
using System.Text.Json;

namespace EasySaveProject.Core.Services
{
    public class BackupService
    {
        private readonly FileService _fileService;
        private readonly StateService _stateService;
        private readonly CryptoService _cryptoService;
        private readonly BusinessSoftwareWatcher _watcher;
        private readonly PauseService _pauseService;
        private readonly PriorityCoordinator _priorityCoordinator;
        private readonly LargeFileTransferGuard _largeFileGuard;
        private readonly ConfigService _configService;

        private readonly List<BackupJob> _jobs = new();
        private readonly ConcurrentDictionary<string, JobController> _activeControllers = new();

        private readonly string _jobsPath = Path.Combine(
            AppContext.BaseDirectory, "Data", "Jobs", "jobs.json");

        public BackupService(
            FileService fileService,
            LogService logService,
            StateService stateService,
            CryptoService cryptoService,
            BusinessSoftwareWatcher watcher,
            PauseService pauseService,
            PriorityCoordinator priorityCoordinator,
            LargeFileTransferGuard largeFileGuard,
            ConfigService configService)
        {
            _fileService = fileService;
            _stateService = stateService;
            _cryptoService = cryptoService;
            _watcher = watcher;
            _pauseService = pauseService;
            _priorityCoordinator = priorityCoordinator;
            _largeFileGuard = largeFileGuard;
            _configService = configService;

            if (File.Exists(_jobsPath))
            {
                var json = File.ReadAllText(_jobsPath);
                var jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
                if (jobs != null)
                    _jobs.AddRange(jobs);
            }
        }

        public void LoadJobs(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);

            _jobs.Clear();
            if (jobs == null)
                throw new Exception("Invalid jobs configuration file");
            _jobs.AddRange(jobs);
        }

        public void UpdateJobType(int index, BackupType type)
        {
            if (index < 0 || index >= _jobs.Count) return;

            var oldJob = _jobs[index];
            _jobs[index] = new BackupJob(oldJob.Name, oldJob.SourcePath, oldJob.TargetPath, type);
            SaveJobs();
        }

        public void AddJob(BackupJob job)
        {
            _jobs.Add(job);
            SaveJobs();
        }

        private void RefreshConfig()
        {
            var config = _configService.Load();
            // BusinessSoftwareWatcher reads config directly — no Update() needed
            _priorityCoordinator.Update(config.PriorityExtensions);
            _largeFileGuard.Update(config.LargeFileThresholdKb);
        }

        /// <summary>
        /// Runs a single job synchronously.
        /// Returns the final status string ("Completed", "Cancelled", "Error").
        /// </summary>
        public string RunJob(int index)
        {
            RefreshConfig();
            ExecuteJobCore(index);
            _pauseService.Reset();
            var finalState = BackupStateHub.ReadAll()
                .FirstOrDefault(s => index >= 0 && index < _jobs.Count && s.BackupName == _jobs[index].Name);
            string status = finalState?.Status ?? "Completed";
            BackupStateHub.Clear();
            return status == "Stopped" ? "Cancelled" : status;
        }

        /// <summary>
        /// Runs multiple jobs in parallel.
        /// Returns a dictionary of job name → final status string.
        /// </summary>
        public async Task<Dictionary<string, string>> RunJobsParallelAsync(List<int> indices)
        {
            var valid = indices.Where(i => i >= 0 && i < _jobs.Count).ToList();
            if (valid.Count == 0) return new();

            RefreshConfig();
            await Task.WhenAll(valid.Select(i => Task.Run(() => ExecuteJobCore(i))));

            _pauseService.Reset();

            var results = new Dictionary<string, string>();
            foreach (var i in valid)
            {
                var name = _jobs[i].Name;
                var state = BackupStateHub.ReadAll().FirstOrDefault(s => s.BackupName == name);
                string status = state?.Status ?? "Completed";
                results[name] = status == "Stopped" ? "Cancelled" : status;
            }

            BackupStateHub.Clear();
            return results;
        }

        public void DeleteJob(int index)
        {
            if (index < 0 || index >= _jobs.Count) return;

            _jobs.RemoveAt(index);
            SaveJobs();
        }

        // ── Per-job control ───────────────────────────────────────────────────

        /// <summary>Returns the active controller for a job by name, or null if not running.</summary>
        public JobController? GetControllerByName(string name)
            => _activeControllers.TryGetValue(name, out var c) ? c : null;

        public bool IsGloballyPaused => _pauseService.IsPaused;

        public void PauseAll()  => _pauseService.Pause();

        public void ResumeAll()
        {
            _pauseService.Resume();
            foreach (var c in _activeControllers.Values) c.Resume();
        }

        public void StopAll()
        {
            _pauseService.Stop();
            foreach (var c in _activeControllers.Values) c.Stop();
        }
        public void TogglePauseJob(string name) { if (_activeControllers.TryGetValue(name, out var c)) c.Toggle(); }
        public void StopJob(string name)        { if (_activeControllers.TryGetValue(name, out var c)) c.Stop(); }

        // ── Execution ─────────────────────────────────────────────────────────

        private void ExecuteJobCore(int index)
        {
            if (index < 0 || index >= _jobs.Count) return;

            var job = _jobs[index];
            var controller = new JobController(job.Name);
            _activeControllers[job.Name] = controller;

            var strategy = BackupStrategyFactory.Create(job.Type);

            try
            {
                strategy.Execute(
                    job,
                    _fileService,
                    LogService.Instance,
                    _stateService,
                    _cryptoService,
                    _watcher,
                    _pauseService,
                    controller,
                    _priorityCoordinator,
                    _largeFileGuard
                );
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError(job.Name, job.SourcePath, job.TargetPath, 0,
                    $"Job failed: {ex.Message}");

                _stateService.Update(new State
                {
                    BackupName = job.Name,
                    Timestamp = DateTime.Now,
                    Status = "Error",
                    TotalFiles = 0,
                    RemainingFiles = 0,
                    TotalSize = 0,
                    RemainingSize = 0,
                    CurrentSourceFile = string.Empty,
                    CurrentTargetFile = string.Empty
                });

                Console.WriteLine($"Error for job '{job.Name}': {ex.Message}");
            }
            finally
            {
                _activeControllers.TryRemove(job.Name, out _);
            }
        }

        private void SaveJobs()
        {
            var directory = Path.GetDirectoryName(_jobsPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var json = JsonSerializer.Serialize(_jobs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_jobsPath, json);
        }

        public IReadOnlyList<BackupJob> GetJobs() => _jobs;
    }
}
