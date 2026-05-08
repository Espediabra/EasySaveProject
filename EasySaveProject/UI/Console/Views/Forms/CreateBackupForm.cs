using EasySaveProject.Core.Localization;
using EasySaveProject.Models;

public class CreateBackupForm
{
    private readonly MainViewModel _viewModel;
    private readonly InputForm _input;
    private readonly BackupTypeForm _typeForm;
    private readonly ConfirmDialog _confirm;
    private readonly LocalizationService _loc;

    public CreateBackupForm(
        MainViewModel viewModel,
        InputForm input,
        BackupTypeForm typeForm,
        ConfirmDialog confirm,
        LocalizationService loc)
    {
        _viewModel = viewModel;
        _input = input;
        _typeForm = typeForm;
        _confirm = confirm;
        _loc = loc;
    }

    public void Show()
    {
        Console.Clear();
        HeaderComponent.Render(_loc.T("Form.CreateTitle"));

        string name = _input.AskRequired(_loc.T("Form.NamePrompt"));
        string source = _input.AskRequired(_loc.T("Form.SourcePrompt"));
        string target = _input.AskRequired(_loc.T("Form.TargetPrompt"));

        var type = _typeForm.Select(_loc.T("Backup.TypeSelection"));

        _viewModel.CreateJob(name, source, target, type);

        Console.WriteLine(_loc.T("Job.Created"));

        bool runNow = _confirm.Ask(_loc.T("Confirm.RunNow"));

        if (runNow)
        {
            int index = _viewModel.GetJobsRaw().Count - 1;

            _viewModel.ExecuteBackup(index);
            Console.WriteLine(_loc.T("Backup.Executed"));
            Console.ReadKey();
        }
    }
}