public static class ConsoleHelper
{
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