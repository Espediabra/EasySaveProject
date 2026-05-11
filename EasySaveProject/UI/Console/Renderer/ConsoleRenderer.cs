public static class ConsoleRenderer
{
    public static void Clear()
    {
        Console.Clear();
    }

    public static void WriteLine(string text = "")
    {
        Console.WriteLine(text);
    }

    public static void Write(string text)
    {
        Console.Write(text);
    }

    public static string ReadLine()
    {
        return Console.ReadLine() ?? string.Empty;
    }

    public static ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        return Console.ReadKey(intercept);
    }

    public static void Pause()
    {
        Console.ReadKey();
    }
}