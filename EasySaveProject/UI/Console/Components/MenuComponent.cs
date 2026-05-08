public class MenuComponent
{
    public int Select(List<string> options)
    {
        int index = 0;
        int startTop = Console.CursorTop;

        while (true)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, startTop);

            for (int i = 0; i < options.Count; i++)
            {
                if (i == index)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }

                Console.Write($"{options[i]}".PadRight(Console.WindowWidth));
                Console.ResetColor();

                if (i < options.Count - 1)
                    ConsoleRenderer.WriteLine();
            }

            var key = ConsoleRenderer.ReadKey(true);

            if (key.Key == ConsoleKey.UpArrow)
                index = (index - 1 + options.Count) % options.Count;

            else if (key.Key == ConsoleKey.DownArrow)
                index = (index + 1) % options.Count;

            else if (key.Key == ConsoleKey.Enter)
            {
                Console.CursorVisible = true;
                return index;
            }
        }
    }
}