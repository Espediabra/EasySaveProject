using EasySaveProject.Core.Localization;

public class BackupMenuView
{
    private readonly MainViewModel _viewModel;
    private readonly MenuComponent _menu;
    private readonly InteractiveMenuComponent _interactiveMenu;
    private readonly LocalizationService _loc;
    private readonly CreateBackupForm _createForm;
    private readonly ConfirmDialog _confirm;
    private readonly BackupTypeForm _typeForm;

    public BackupMenuView(
    MainViewModel viewModel,
    MenuComponent menu,
    InteractiveMenuComponent interactiveMenu,
    LocalizationService loc,
    CreateBackupForm createForm,
    ConfirmDialog confirm,
    BackupTypeForm typeForm)
    {
        _viewModel = viewModel;
        _menu = menu;
        _interactiveMenu = interactiveMenu;
        _loc = loc;
        _createForm = createForm;
        _confirm = confirm;
        _typeForm = typeForm;
    }
    public void Show()
    {
        while (true)
        {
            Console.Clear();

            HeaderComponent.Render(_loc.T("Backup.Menu"));

            var jobs = _viewModel.GetBackupNames();

            if (jobs.Count == 0)
            {
                bool shouldExit = HandleNoJobs();

                if (shouldExit)
                    return;

                continue;
            }

            var result = _interactiveMenu.Show(
                () => HeaderComponent.Render(_loc.T("Backup.Menu")),
                jobs,
                _loc.T("Backup.CreateNew"),
                _loc.T("Backup.RunSelected"),
                _loc.T("Return")
            );

            if (result.ActionType == InteractiveActionType.Create)
            {
                CreateNewJobFlow();
                continue;
            }

            if (result.ActionType == InteractiveActionType.Back)
            {
                return;
            }

            if (result.ActionType == InteractiveActionType.Run)
            {
                _viewModel.ExecuteMultipleBackups(result.SelectedIndices);

                Console.WriteLine(_loc.T("Backup.Executed"));
                Console.ReadKey();
                continue;
            }

            if (result.IsMultiSelection)
            {
                _viewModel.ExecuteMultipleBackups(
                    result.SelectedIndices
                );

                Console.WriteLine(_loc.T("Backup.Executed"));
                Console.ReadKey();

                continue;
            }

            if (result.ActionType == InteractiveActionType.Job)
            {
                OpenJobMenu(result.SelectedIndex, result.HasActiveSelection);
            }
        }
    }


    private void OpenJobMenu(int index, bool HasActiveSelection)
    {
        while (true)
        {
            Console.Clear();
            HeaderComponent.Render(_loc.T("Job.Options"));

            var options = new List<string>
            {
                _loc.T("Backup.Run"),
                _loc.T("View.Details"),
                _loc.T("Backup.ChangeType"),
                _loc.T("Backup.Delete"),
                _loc.T("Return")
            };

            var disabled = new HashSet<int>();

            if (HasActiveSelection)
            {
                disabled.Add(0); // Run
                disabled.Add(2); // Change type
                disabled.Add(3); // Delete
            }

            int choice = _menu.Select(options, disabled);

            switch (choice)
            {
                case 0:
                    if (!HasActiveSelection)
                    {
                        _viewModel.ExecuteBackup(index);
                        Console.WriteLine(_loc.T("Backup.Executed"));
                        Console.ReadKey();
                    }
                    break;
                case 1:
                    ShowJobDetails(index);
                    break;
                case 2:
                    if (!HasActiveSelection)
                    {
                        ChangeBackupType(index);
                    }
                    break;
                case 3:
                    if (!HasActiveSelection)
                    {
                        DeleteBackup(index);
                        return;
                    }
                    break;
                case 4:
                    return;
            }
        }

    }

    private bool HandleNoJobs()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Backup.NoBackupJobs"));

        var options = new List<string>
        {
            _loc.T("Backup.CreateA"),
            _loc.T("Back")
        };

        int choice = _menu.Select(options);

        switch (choice)
        {
            case 0:
                _createForm.Show();
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
        HeaderComponent.Render(_loc.T("Job.Detail"));

        Console.WriteLine($"{_loc.T("Job.Name")}: {job.Name}");
        Console.WriteLine($"{_loc.T("Job.Source")}: {job.SourcePath}");
        Console.WriteLine($"{_loc.T("Job.Target")}: {job.TargetPath}");
        Console.WriteLine($"{_loc.T("Job.Type")}: {job.Type}");

        Console.ReadKey();
    }

    private void CreateNewJobFlow()
    {
        try
        {
            _createForm.Show();
        }
        catch (InvalidOperationException)
        {
            HandleOverwrite();
        }
    }

    private void HandleOverwrite()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Job.MaxReached"));

        var jobs = _viewModel.GetBackupNames();

        jobs.Add(_loc.T("Cancel"));

        int choice = _menu.Select(jobs);

        if (choice == jobs.Count - 1)
            return;

        _viewModel.DeleteJob(choice);
        _createForm.Show();
    }

    private void ChangeBackupType(int index)
    {
        var type = _typeForm.Select(_loc.T("Backup.TypeSelection"));

        _viewModel.ChangeJobType(index, type);

        Console.WriteLine(_loc.T("Backup.TypeUpdated"));
        Console.ReadKey();
    }

    private void DeleteBackup(int index)
    {
        bool confirmed = _confirm.Ask(_loc.T("Confirm.Delete"));

        if (confirmed)
        {
            _viewModel.DeleteJob(index);
            Console.WriteLine(_loc.T("Backup.Delete"));
            Console.ReadKey();
        }
    }
}