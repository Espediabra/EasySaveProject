namespace EasySaveProject.Helpers;
using EasySaveProject.Core.Localization;

public class RunCliFlow
{
    private readonly MainViewModel _viewModel;
    private readonly CreateBackupForm _createForm;
    private readonly MenuComponent _menu;
    private readonly LocalizationService _loc;

    private const int MAX_BACKUPS = 5;

    public RunCliFlow(
        MainViewModel viewModel,
        CreateBackupForm createForm,
        MenuComponent menu,
        LocalizationService loc)
    {
        _viewModel = viewModel;
        _createForm = createForm;
        _menu = menu;
        _loc = loc;
    }

    public void Execute(string[] args)
    {
        var indices = ArgumentParser.Parse(args[0]);

        var jobs = _viewModel.GetJobsRaw();

        var validJobs = new List<int>();
        var missingJobs = new List<int>();

        foreach (var index in indices)
        {
            int realIndex = index - 1;

            if (realIndex < 0 || realIndex >= jobs.Count)
                missingJobs.Add(index);
            else
                validJobs.Add(realIndex);
        }

        if (missingJobs.Count > 0)
        {
            HandleMissingJobs(missingJobs);

            // reload jobs after creations
            jobs = _viewModel.GetJobsRaw();

            foreach (var index in missingJobs)
            {
                int realIndex = index - 1;

                if (realIndex >= 0 &&
                    realIndex < jobs.Count &&
                    !validJobs.Contains(realIndex))
                {
                    validJobs.Add(realIndex);
                }
            }
        }

        Console.Clear();

        HeaderComponent.Render(_loc.T("Cli.ResultTitle"));

        foreach (var job in validJobs)
        {
            _viewModel.ExecuteBackup(job);

            var backup = _viewModel.GetJob(job);

            Console.WriteLine(
                $"{_loc.T("Cli.BackupExecuted")} : {backup.Name}"
            );
        }

        Console.ReadKey();
    }

    private void HandleMissingJobs(List<int> missing)
    {
        var created = new HashSet<int>();

        while (true)
        {
            var options = new List<string>();
            var disabled = new HashSet<int>();

            for (int i = 0; i < missing.Count; i++)
            {
                if (created.Contains(i))
                {
                    options.Add(
                        $"{_loc.T("Cli.BackupCreated")} {missing[i]}"
                    );

                    disabled.Add(i);
                }
                else
                {
                    options.Add(
                        $"{_loc.T("Cli.CreateBackup")} {missing[i]}"
                    );
                }
            }

            options.Add(_loc.T("Cli.SkipMissingBackups"));

            Console.Clear();

            HeaderComponent.Render(
                _loc.T("Cli.MissingBackupsTitle")
            );

            int choice = _menu.Select(options, disabled);

            // skip
            if (choice == options.Count - 1)
                return;

            // already created
            if (created.Contains(choice))
                continue;

            var jobs = _viewModel.GetJobsRaw();

            // max backups reached
            if (jobs.Count >= MAX_BACKUPS)
            {
                HandleOverwrite();
            }
            else
            {
                Console.Clear();

                HeaderComponent.Render(
                    $"{_loc.T("Cli.CreateBackup")} {missing[choice]}"
                );

                _createForm.Show(false);
            }

            created.Add(choice);

            // all created
            if (created.Count == missing.Count)
                return;
        }
    }

    private void HandleOverwrite()
    {
        Console.Clear();

        HeaderComponent.Render(
            _loc.T("Job.MaxReached")
        );

        var jobs = _viewModel
            .GetBackupNames();

        jobs.Add(_loc.T("Cancel"));

        int choice = _menu.Select(jobs);

        if (choice == jobs.Count - 1)
            return;

        _viewModel.DeleteJob(choice);

        _createForm.Show(false);
    }
}