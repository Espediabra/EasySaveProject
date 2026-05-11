public static class TableComponent
{
    public static void Render(List<string[]> rows)
    {
        foreach (var row in rows)
        {
            ConsoleRenderer.WriteLine(string.Join(" | ", row));
        }
    }
}