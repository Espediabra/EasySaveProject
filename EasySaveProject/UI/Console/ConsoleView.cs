using EasySaveProject.Models;
using EasySaveProject.Core.Localization;

public class ConsoleView
{
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menu;
    private readonly LocalizationService _loc = new();

    public ConsoleView(MainViewModel viewModel, MenuService menu, LocalizationService loc)
    {
        _viewModel = viewModel;
        _menu = menu;
        _loc = loc;
    }

    public void ShowBackupMenu()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("Backup.Menu"));

            var jobs = _viewModel.GetBackupNames();

            if (jobs.Count == 0)
            {
                bool shouldExit = HandleNoJobs();

                if (shouldExit)
                    return;

                continue;
            }

            var options = new List<string>();

            options.AddRange(jobs);

            options.Add(_loc.T("Backup.CreateNew"));
            options.Add(_loc.T("Return"));

            int choice = _menu.ShowMenu(options);

            if (choice == options.Count - 1)
                return;

            if (choice == options.Count - 2)
            {
                CreateNewJobFlow();
                continue;
            }

            OpenJobMenu(choice);
        }
    }

    private void OpenJobMenu(int index)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header(_loc.T("Job.Options"));

            var options = new List<string>
        {
            _loc.T("Backup.Run"),
            _loc.T("View.Details"),
            _loc.T("Backup.ChangeType"),
            _loc.T("Backup.Delete"),
            _loc.T("Return")
        };

            int choice = _menu.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    _viewModel.ExecuteBackup(index);
                    Console.WriteLine(_loc.T("Backup.Executed"));
                    Console.ReadKey();
                    break;

                case 1:
                    ShowJobDetails(index);
                    break;

                case 2:
                    ChangeBackupType(index);
                    break;

                case 3:
                    DeleteBackup(index);
                    return;

                case 4:
                    return;
            }
        }
    }

    private bool HandleNoJobs()
    {
        Console.Clear();
        ConsoleHelper.Header(_loc.T("Backup.NoBackupJobs"));

        var options = new List<string>
    {
        _loc.T("Backup.CreateA"),
        _loc.T("Back")
    };

        int choice = _menu.ShowMenu(options);

        switch (choice)
        {
            case 0:
                CreateJobForm();
                return false;

            case 1:
                return true;
        }

        return false;
    }

    private void ShowJobDetails(int index)
    {
        var job = _viewModel.GetJob(index);

        Console.Clear();
        ConsoleHelper.Header(_loc.T("Job.Detail"));

        Console.WriteLine($"{_loc.T("Job.Name")}: {job.Name}");
        Console.WriteLine($"{_loc.T("Job.Source")}: {job.SourcePath}");
        Console.WriteLine($"{_loc.T("Job.Target")}: {job.TargetPath}");
        Console.WriteLine($"{_loc.T("Job.Type")}: {job.Type}");

        Console.ReadKey();
    }

    private void CreateNewJobFlow()
    {
        var jobs = _viewModel.GetJobsRaw();

        if (jobs.Count >= 5)
        {
            HandleOverwrite();
            return;
        }

        CreateJobForm();
    }

    private void HandleOverwrite()
    {
        Console.Clear();
        ConsoleHelper.Header(_loc.T("Job.MaxReached"));

        var jobs = _viewModel.GetBackupNames();

        jobs.Add(_loc.T("Cancel"));

        int choice = _menu.ShowMenu(jobs);

        if (choice == jobs.Count - 1)
            return;

        _viewModel.DeleteJob(choice);
        CreateJobForm();
    }

    private void CreateJobForm()
    {
        Console.Clear();
        ConsoleHelper.Header(_loc.T("Form.CreateTitle"));

        string name = AskRequired(_loc.T("Form.NamePrompt"));
        string source = AskRequired(_loc.T("Form.SourcePrompt"));
        string target = AskRequired(_loc.T("Form.TargetPrompt"));

        var type = AskBackupType(_loc.T("Backup.TypeSelection"));

        _viewModel.CreateJob(name, source, target, type);

        Console.WriteLine(_loc.T("Job.Created"));

        bool runNow = Confirm(_loc.T("Confirm.RunNow"));

        if (runNow)
        {
            int index = _viewModel.GetJobsRaw().Count - 1;

            _viewModel.ExecuteBackup(index);
            Console.WriteLine(_loc.T("Backup.Executed"));
            Console.ReadKey();
        }
    }

    private string AskRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
                return input;

            Console.WriteLine(_loc.T("Form.EmptyError"));
        }
    }

    private void ChangeBackupType(int index)
    {
        var type = AskBackupType(_loc.T("Backup.TypeSelection"));

        _viewModel.ChangeJobType(index, type);

        Console.WriteLine(_loc.T("Backup.TypeUpdated"));
        Console.ReadKey();
    }

    private void DeleteBackup(int index)
    {
        bool confirmed = Confirm(_loc.T("Confirm.Delete"));

        if (confirmed)
        {
            _viewModel.DeleteJob(index);
            Console.WriteLine(_loc.T("Backup.Delete"));
            Console.ReadKey();
        }
    }

    private BackupType AskBackupType(string title)
    {
        Console.Clear();
        ConsoleHelper.Header(title);

        var options = new List<string>
    {
        _loc.T("Backup.TypeFull"),
        _loc.T("Backup.TypeDifferential")
    };

        int choice = _menu.ShowMenu(options);

        var type = choice switch
        {
            0 => BackupType.Full,
            1 => BackupType.Differential,
            _ => BackupType.Full
        };

        return type;
    }

    private bool Confirm(string title)
    {
        Console.Clear();
        ConsoleHelper.Header(title);

        var options = new List<string> { _loc.T("Confirm.Yes"), _loc.T("Confirm.No") };

        return (_menu.ShowMenu(options) == 0);
    }
}