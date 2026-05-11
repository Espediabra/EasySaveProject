public class MenuComponent
{
    public int Select(List<string> options, HashSet<int>? disabled = null)
    {
        int index = 0;
        int startTop = Console.CursorTop;

        if (disabled != null && disabled.Contains(index))
        {
            index = FindNextEnabled(index, options.Count, disabled, forward: true);
        }

        while (true)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, startTop);

            for (int i = 0; i < options.Count; i++)
            {
                bool isDisabled = disabled?.Contains(i) == true;
                bool isSelected = i == index;

                if (isSelected)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (isDisabled)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }

                Console.Write(options[i].PadRight(Console.WindowWidth));
                Console.ResetColor();

                if (i < options.Count - 1)
                    ConsoleRenderer.WriteLine();
            }

            var key = ConsoleRenderer.ReadKey(true);

            if (key.Key == ConsoleKey.UpArrow)
            {
                index = FindNextEnabled(index, options.Count, disabled, forward: false);
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                index = FindNextEnabled(index, options.Count, disabled, forward: true);
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                if (disabled?.Contains(index) == true)
                    continue;

                Console.CursorVisible = true;
                return index;
            }
        }
    }

    private int FindNextEnabled(int current, int count, HashSet<int>? disabled, bool forward)
    {
        int next = current;

        do
        {
            next = forward
                ? (next + 1) % count
                : (next - 1 + count) % count;

        } while (disabled?.Contains(next) == true);

        return next;
    }
}