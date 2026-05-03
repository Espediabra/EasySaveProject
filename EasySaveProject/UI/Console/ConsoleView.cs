using EasySaveProject.Models;

public class ConsoleView
{
    private readonly MainViewModel _viewModel;
    private readonly MenuService _menu;

    public ConsoleView(MainViewModel viewModel, MenuService menu)
    {
        _viewModel = viewModel;
        _menu = menu;
    }

    public void ShowBackupMenu()
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.Header("Backup Menu");

            var jobs = _viewModel.GetBackupNames();

            var options = new List<string>();

            options.AddRange(jobs);

            options.Add("Create new backup");
            options.Add("Return");

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
            ConsoleHelper.Header("Job Options");

            var options = new List<string>
        {
            "Run backup",
            "View details",
            "Change backup type",
            "Delete backup",
            "Return"
        };

            int choice = _menu.ShowMenu(options);

            switch (choice)
            {
                case 0:
                    _viewModel.ExecuteBackup(index);
                    Console.WriteLine("Backup executed.");
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

    private void HandleNoJobs()
    {
        Console.WriteLine("No backup jobs available.");

        var options = new List<string>
    {
        "Create a backup job",
        "Back"
    };

        int choice = _menu.ShowMenu(options);

        switch (choice)
        {
            case 0:
                CreateJobForm();
                break;

            case 1:
                return;
        }
    }

    private void ShowJobDetails(int index)
    {
        var job = _viewModel.GetJob(index);

        Console.Clear();
        ConsoleHelper.Header("Job Details");

        Console.WriteLine($"Name: {job.Name}");
        Console.WriteLine($"Source: {job.SourcePath}");
        Console.WriteLine($"Target: {job.TargetPath}");
        Console.WriteLine($"Type: {job.Type}");

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
        ConsoleHelper.Header("Maximum jobs reached (5). Please select a job to overwrite.");

        var jobs = _viewModel.GetBackupNames();

        jobs.Add("Cancel");

        int choice = _menu.ShowMenu(jobs);

        if (choice == jobs.Count - 1)
            return;

        _viewModel.DeleteJob(choice);
        CreateJobForm();
    }

    private void CreateJobForm()
    {
        Console.Clear();
        ConsoleHelper.Header("Create Backup Job");

        string name = AskRequired("Name: ");
        string source = AskRequired("Source path: ");
        string target = AskRequired("Target path: ");

        var type = AskBackupType("Select Backup Type");

        _viewModel.CreateJob(name, source, target, type);

        Console.WriteLine("Job created successfully.");

        bool runNow = Confirm("Do you want to run it now?");

        if (runNow)
        {
            // last job index (newly added one)
            int index = _viewModel.GetJobsRaw().Count - 1;

            _viewModel.ExecuteBackup(index);
            Console.WriteLine("Backup executed.");
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

            Console.WriteLine("Value cannot be empty.");
        }
    }

    private void ChangeBackupType(int index)
    {
        var type = AskBackupType("Select Backup Type");

        _viewModel.ChangeJobType(index, type);

        Console.WriteLine("Backup type updated.");
        Console.ReadKey();
    }

    private void DeleteBackup(int index)
    {
        bool confirmed = Confirm("Confirm deletion");

        if (confirmed)
        {
            _viewModel.DeleteJob(index);
            Console.WriteLine("Backup deleted.");
            Console.ReadKey();
        }
    }

    private BackupType AskBackupType(string title)
    {
        Console.Clear();
        ConsoleHelper.Header(title);

        var options = new List<string>
    {
        "Full",
        "Differential"
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

        var options = new List<string> { "Yes", "No" };

        return (_menu.ShowMenu(options) == 0);
    }
}