public static class HeaderComponent
{
    public static void Render(string text)
    {
        int width = Console.WindowWidth;
        string line = new string('=', width);

        ConsoleRenderer.WriteLine();
        ConsoleRenderer.WriteLine(line);
        ConsoleRenderer.WriteLine(CenterText(text, width));
        ConsoleRenderer.WriteLine(line);
        ConsoleRenderer.WriteLine();
    }

    private static string CenterText(string text, int width)
    {
        return text.PadLeft((width + text.Length) / 2);
    }
}