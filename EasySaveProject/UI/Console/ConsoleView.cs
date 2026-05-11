// // using EasySaveProject.Models;
// // using EasySaveProject.Core.Localization;

// // public class ConsoleView
// // {
// //     private readonly MainViewModel _viewModel;
// //     private readonly MenuComponent _menu;
// //     private readonly LocalizationService _loc = new();

// //     public ConsoleView(MainViewModel viewModel, MenuComponent menu, LocalizationService loc)
// //     {
// //         _viewModel = viewModel;
// //         _menu = menu;
// //         _loc = loc;
// //     }

// //     public void ShowBackupMenu()
// //     {
// //         while (true)
// //         {
// //             Console.Clear();
// //             HeaderComponent.Render(_loc.T("Backup.Menu"));

// //             var jobs = _viewModel.GetBackupNames();

// //             if (jobs.Count == 0)
// //             {
// //                 bool shouldExit = HandleNoJobs();

// //                 if (shouldExit)
// //                     return;

// //                 continue;
// //             }

// //             var options = new List<string>();

// //             options.AddRange(jobs);

// //             options.Add(_loc.T("Backup.RunMultiple"));
// //             options.Add(_loc.T("Backup.CreateNew"));
// //             options.Add(_loc.T("Return"));

// //             int choice = _menu.Select(options);

// //             if (choice == options.Count - 1)
// //                 return;

// //             if (choice == options.Count - 2)
// //             {
// //                 CreateNewJobFlow();
// //                 continue;
// //             }

// //             // Lancer plusieurs sauvegardes
// //             if (choice == options.Count - 3)
// //             {
// //                 RunMultipleJobsFlow();
// //                 continue;
// //             }

// //             OpenJobMenu(choice);
// //         }
// //     }

// //     private void RunMultipleJobsFlow()
// //     {
// //         var jobs = _viewModel.GetBackupNames();

// //         if (jobs.Count == 0)
// //         {
// //             Console.Clear();
// //             Console.WriteLine(_loc.T("Backup.NoBackupJobs"));
// //             Console.ReadKey();
// //             return;
// //         }

// //         // Tableau de cases à cocher
// //         var selection = new bool[jobs.Count];

// //         while (true)
// //         {
// //             Console.Clear();
// //             HeaderComponent.Render(_loc.T("Backup.SelectMultiple"));

// //             // Affiche chaque job avec son état coché/décoché
// //             for (int i = 0; i < jobs.Count; i++)
// //             {
// //                 string coche = selection[i] ? "[X]" : "[ ]";
// //                 Console.WriteLine($"  {i + 1}. {coche} {jobs[i]}");
// //             }

// //             Console.WriteLine();
// //             Console.WriteLine(_loc.T("Backup.SelectInstruction"));
// //             Console.WriteLine(_loc.T("Backup.ConfirmInstruction"));
// //             Console.WriteLine(_loc.T("Backup.CancelInstruction"));
// //             Console.WriteLine();
// //             Console.Write("> ");

// //             string? input = Console.ReadLine()?.Trim();
// //             if (string.IsNullOrEmpty(input))
// //             {
// //                 var indices = new List<int>();

// //                 for (int i = 0; i < selection.Length; i++)
// //                     if (selection[i])
// //                         indices.Add(i);

// //                 if (indices.Count == 0)
// //                 {
// //                     Console.WriteLine(_loc.T("Backup.NoneSelected"));
// //                     Console.ReadKey();
// //                     return;
// //                 }

// //                 _viewModel.ExecuteMultipleBackups(indices);
// //                 Console.WriteLine(_loc.T("Backup.Executed"));
// //                 Console.ReadKey();
// //                 return;
// //             }

// //             if (input.ToLower() == "q")
// //                 return;

// //             if (int.TryParse(input, out int choix) && choix >= 1 && choix <= jobs.Count)
// //             {
// //                 selection[choix - 1] = !selection[choix - 1];
// //             }
// //             else
// //             {
// //                 Console.WriteLine(_loc.T("Form.EmptyError"));
// //                 Console.ReadKey();
// //             }
// //         }
// //     }

// //     private void OpenJobMenu(int index)
// //     {
// //         while (true)
// //         {
// //             Console.Clear();
// //             HeaderComponent.Render(_loc.T("Job.Options"));

// //             var options = new List<string>
// //             {
// //                 _loc.T("Backup.Run"),
// //                 _loc.T("View.Details"),
// //                 _loc.T("Backup.ChangeType"),
// //                 _loc.T("Backup.Delete"),
// //                 _loc.T("Return")
// //             };

// //             int choice = _menu.Select(options);

// //             switch (choice)
// //             {
// //                 case 0:
// //                     _viewModel.ExecuteBackup(index);
// //                     Console.WriteLine(_loc.T("Backup.Executed"));
// //                     Console.ReadKey();
// //                     break;

// //                 case 1:
// //                     ShowJobDetails(index);
// //                     break;

// //                 case 2:
// //                     ChangeBackupType(index);
// //                     break;

// //                 case 3:
// //                     DeleteBackup(index);
// //                     return;

// //                 case 4:
// //                     return;
// //             }
// //         }
// //     }

// //     private bool HandleNoJobs()
// //     {
// //         Console.Clear();
// //         HeaderComponent.Render(_loc.T("Backup.NoBackupJobs"));

// //         var options = new List<string>
// //         {
// //             _loc.T("Backup.CreateA"),
// //             _loc.T("Back")
// //         };

// //         int choice = _menu.Select(options);

// //         switch (choice)
// //         {
// //             case 0:
// //                 CreateJobForm();
// //                 return false;

// //             case 1:
// //                 return true;
// //         }

// //         return false;
// //     }

// //     private void ShowJobDetails(int index)
// //     {
// //         var job = _viewModel.GetJob(index);

// //         Console.Clear();
// //         HeaderComponent.Render(_loc.T("Job.Detail"));

// //         Console.WriteLine($"{_loc.T("Job.Name")}: {job.Name}");
// //         Console.WriteLine($"{_loc.T("Job.Source")}: {job.SourcePath}");
// //         Console.WriteLine($"{_loc.T("Job.Target")}: {job.TargetPath}");
// //         Console.WriteLine($"{_loc.T("Job.Type")}: {job.Type}");

// //         Console.ReadKey();
// //     }

// //     private void CreateNewJobFlow()
// //     {
// //         var jobs = _viewModel.GetJobsRaw();

// //         if (jobs.Count >= 5)
// //         {
// //             HandleOverwrite();
// //             return;
// //         }

// //         CreateJobForm();
// //     }

// //     private void HandleOverwrite()
// //     {
// //         Console.Clear();
// //         HeaderComponent.Render(_loc.T("Job.MaxReached"));

// //         var jobs = _viewModel.GetBackupNames();

// //         jobs.Add(_loc.T("Cancel"));

// //         int choice = _menu.Select(jobs);

// //         if (choice == jobs.Count - 1)
// //             return;

// //         _viewModel.DeleteJob(choice);
// //         CreateJobForm();
// //     }

// //     private void CreateJobForm()
// //     {
// //         Console.Clear();
// //         HeaderComponent.Render(_loc.T("Form.CreateTitle"));

// //         string name = AskRequired(_loc.T("Form.NamePrompt"));
// //         string source = AskRequired(_loc.T("Form.SourcePrompt"));
// //         string target = AskRequired(_loc.T("Form.TargetPrompt"));

// //         var type = AskBackupType(_loc.T("Backup.TypeSelection"));

// //         _viewModel.CreateJob(name, source, target, type);

// //         Console.WriteLine(_loc.T("Job.Created"));

// //         bool runNow = Confirm(_loc.T("Confirm.RunNow"));

// //         if (runNow)
// //         {
// //             int index = _viewModel.GetJobsRaw().Count - 1;

// //             _viewModel.ExecuteBackup(index);
// //             Console.WriteLine(_loc.T("Backup.Executed"));
// //             Console.ReadKey();
// //         }
// //     }

// //     private string AskRequired(string prompt)
// //     {
// //         while (true)
// //         {
// //             Console.Write(prompt);
// //             string? input = Console.ReadLine();

// //             if (!string.IsNullOrWhiteSpace(input))
// //                 return input;

// //             Console.WriteLine(_loc.T("Form.EmptyError"));
// //         }
// //     }

// //     private void ChangeBackupType(int index)
// //     {
// //         var type = AskBackupType(_loc.T("Backup.TypeSelection"));

// //         _viewModel.ChangeJobType(index, type);

// //         Console.WriteLine(_loc.T("Backup.TypeUpdated"));
// //         Console.ReadKey();
// //     }

// //     private void DeleteBackup(int index)
// //     {
// //         bool confirmed = Confirm(_loc.T("Confirm.Delete"));

// //         if (confirmed)
// //         {
// //             _viewModel.DeleteJob(index);
// //             Console.WriteLine(_loc.T("Backup.Delete"));
// //             Console.ReadKey();
// //         }
// //     }

// //     private BackupType AskBackupType(string title)
// //     {
// //         Console.Clear();
// //         HeaderComponent.Render(title);

// //         var options = new List<string>
// //         {
// //             _loc.T("Backup.TypeFull"),
// //             _loc.T("Backup.TypeDifferential")
// //         };

// //         int choice = _menu.Select(options);

// //         var type = choice switch
// //         {
// //             0 => BackupType.Full,
// //             1 => BackupType.Differential,
// //             _ => BackupType.Full
// //         };

// //         return type;
// //     }

// //     private bool Confirm(string title)
// //     {
// //         Console.Clear();
// //         HeaderComponent.Render(title);

// //         var options = new List<string> { _loc.T("Confirm.Yes"), _loc.T("Confirm.No") };

// //         return (_menu.Select(options) == 0);
// //     }
// // }

// // // public class ConsoleView
// // // {
// // //     private readonly MainMenuView _mainMenu;

// // //     public ConsoleView(MainMenuView mainMenu)
// // //     {
// // //         _mainMenu = mainMenu;
// // //     }

// // //     public void Run()
// // //     {
// // //         _mainMenu.Show();
// // //     }
// // // }