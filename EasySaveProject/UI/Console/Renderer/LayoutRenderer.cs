public static class LayoutRenderer
{
    public static void RenderBox(string title, Action content)
    {
        ConsoleRenderer.Clear();

        ConsoleRenderer.WriteLine("==================================");
        ConsoleRenderer.WriteLine($"   {title}");
        ConsoleRenderer.WriteLine("==================================");
        ConsoleRenderer.WriteLine();

        content();

        ConsoleRenderer.WriteLine();
        ConsoleRenderer.WriteLine("==================================");
    }
}