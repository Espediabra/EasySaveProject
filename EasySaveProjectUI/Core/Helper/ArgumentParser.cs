namespace EasySaveProject.Helpers;

public static class ArgumentParser
{
    public static List<int> Parse(string arg)
    {
        // Format tiret
        if (arg.StartsWith("-"))
            return ParseRange(arg);

        // Format point-virgule 
        if (arg.Contains(';'))
            return ParseList(arg);

        // Valeur unique
        if (int.TryParse(arg, out int single))
            return new List<int> { single };

        Console.WriteLine($"Argument non reconnu : {arg}");
        return new List<int>();
    }

    private static List<int> ParseRange(string arg)
    {
        var parts = arg.Split('-');

        if (parts.Length != 2
            || !int.TryParse(parts[0], out int start)
            || !int.TryParse(parts[1], out int end))
        {
            Console.WriteLine($"Format de plage invalide : {arg}");
            return new List<int>();
        }

        return Enumerable.Range(start, end - start + 1).ToList();
    }

    private static List<int> ParseList(string arg)
    {
        var result = new List<int>();

        foreach (var part in arg.Split(';'))
        {
            if (int.TryParse(part.Trim(), out int index))
                result.Add(index);
            else
                Console.WriteLine($"Valeur ignorée (non numérique) : {part}");
        }

        return result;
    }
}