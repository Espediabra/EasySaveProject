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
        Console.Clear();
        ConsoleHelper.Header("Select Backup");

        var jobs = _viewModel.GetBackupNames();

        if (jobs.Count == 0)
        {
            Console.WriteLine("No backup jobs available.");
            Console.ReadKey();
            return;
        }

        int choice = _menu.ShowMenu(jobs);

        _viewModel.ExecuteBackup(choice);
    }
}