public static class ConsoleHelper
{
    public static void Header(string text)
    {
        int width = Console.WindowWidth;
        string line = new string('=', width);

        Console.WriteLine("\n" + line);
        Console.WriteLine(CenterText(text, width));
        Console.WriteLine(line + "\n");
    }

    public static string CenterText(string text, int width)
    {
        return text.PadLeft((width + text.Length) / 2);
    }

    public static void WriteLineWithWrap(string text)
    {
        int width = Console.WindowWidth;
        int pos = 0;

        while (pos < text.Length)
        {
            int len = Math.Min(width, text.Length - pos);
            string part = text.Substring(pos, len);

            Console.WriteLine(part);
            pos += len;
        }
    }
}