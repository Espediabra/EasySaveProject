using EasySaveProject.Core.Localization;
public class InteractiveMenuComponent
{
    private int _index;
    private readonly LocalizationService _loc;
    private readonly HashSet<int> _selected = new();

    public InteractiveMenuComponent(LocalizationService loc)
    {
        _loc = loc;
    }

    public InteractiveMenuResult Show(
        Action renderHeader,
        List<string> jobs,
        string createLabel,
        string runLabel,
        string backLabel)
    {
        // _index = 0;
        // _selected.Clear();

        int startTop = Console.CursorTop;

        while (true)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, startTop);

            // Console.WriteLine();

            // JOBS
            for (int i = 0; i < jobs.Count; i++)
            {
                bool isCurrent = i == _index;
                bool isSelected = _selected.Contains(i);

                string prefix = isSelected ? "[X]" : "[ ]";

                if (isCurrent)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                Console.WriteLine($"{prefix} {jobs[i]}".PadRight(Console.WindowWidth));

                Console.ResetColor();
            }

            Console.WriteLine();

            int createIndex = jobs.Count;
            int runIndex = jobs.Count + 1;
            int backIndex = jobs.Count + 2;

            // CREATE
            RenderOption(createLabel, createIndex);

            // RUN
            bool canRun = _selected.Count > 0;
            RenderOption(runLabel, runIndex, canRun);

            // BACK
            RenderOption(backLabel, backIndex);

            Console.WriteLine();
            Console.WriteLine(_loc.T("Menu.Controls"));

            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    _index = (_index - 1 + jobs.Count + 3) % (jobs.Count + 3);
                    break;

                case ConsoleKey.DownArrow:
                    _index = (_index + 1) % (jobs.Count + 3);
                    break;

                case ConsoleKey.Spacebar:
                    if (_index < jobs.Count)
                        ToggleSelection(_index);
                    break;

                case ConsoleKey.Enter:

                    if (_index == createIndex)
                    {
                        return new InteractiveMenuResult
                        {
                            ActionType = InteractiveActionType.Create
                        };
                    }

                    if (_index == runIndex)
                    {
                        if (_selected.Count == 0)
                            break;

                        var selectedCopy = _selected.ToList();
                        _selected.Clear();
                        return new InteractiveMenuResult
                        {
                            ActionType = InteractiveActionType.Run,
                            IsMultiSelection = true,
                            SelectedIndices = selectedCopy,
                            HasActiveSelection = true
                        };
                    }

                    if (_index == backIndex)
                    {
                        return new InteractiveMenuResult
                        {
                            ActionType = InteractiveActionType.Back
                        };
                    }

                    if (_index < jobs.Count)
                    {
                        return new InteractiveMenuResult
                        {
                            ActionType = InteractiveActionType.Job,
                            SelectedIndex = _index,
                            HasActiveSelection = _selected.Count > 0
                        };
                    }

                    break;
            }
        }
    }

    private void ToggleSelection(int index)
    {
        if (_selected.Contains(index))
            _selected.Remove(index);
        else
            _selected.Add(index);
    }

    private void RenderOption(string text, int index, bool enabled = true)
    {
        if (_index == index)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        else if (!enabled)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        Console.WriteLine(text.PadRight(Console.WindowWidth));

        Console.ResetColor();
    }
}