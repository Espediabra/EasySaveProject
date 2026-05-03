class Program
{
    static void Header(string textToCenter)
    {
        int windowWidth = Console.WindowWidth;
        string separation = new string('=', windowWidth);

        Console.WriteLine($"\n{separation}");
        Console.WriteLine(CenterText(windowWidth, textToCenter));
        Console.WriteLine($"{separation}\n");

        return;
    }

    static string CenterText(int windowWidth, string textToCenter)
    {
        return ($"{textToCenter}".PadLeft((windowWidth + $"{textToCenter}".Length) / 2));
    }

    static void WriteLineWithWrap(string text)
    {
        int windowWidth = Console.WindowWidth;
        int pos = 0;

        while (pos < text.Length)
        {
            int remainingLength = text.Length - pos;
            int segmentLength = Math.Min(windowWidth, remainingLength);

            string segment = text.Substring(pos, segmentLength);

            if (segmentLength == windowWidth && pos + segmentLength < text.Length)
            {
                int lastSpaceIndex = segment.LastIndexOf(' ');

                if (lastSpaceIndex > 0)
                {
                    segmentLength = lastSpaceIndex;
                    segment = text.Substring(pos, segmentLength);
                }
            }
            Console.WriteLine(segment.Trim());
            pos += segmentLength;
        }
    }

    static void Main()
    // static async Task Main()
    // await Task.Delay(5000);
    {
        var configService = new ConfigService();
        var config = configService.Load();

        Console.Clear();
        Header("WELCOME TO THE APPLICATION EASYLOG");
        WriteLineWithWrap($"This application will allow you to make save by moving a folder/file to another file. \n\nPress 'Enter' or wait 10 sec to continue.");

        DateTime startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalMilliseconds < 10000)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
        }
        Console.Clear();



        if (string.IsNullOrWhiteSpace(config.Langage))
        {
            Header("First you need to choose your langage.");
            WriteLineWithWrap("Disclaimer : the langage selected will affect all the interface console langage. However this langage can be modified whenever you want in the settings option later on...");
            Console.Write("\nChoose Langage (en/fr): ");
            config.Langage = Console.ReadLine() ?? "en";
            configService.Save(config);
        }

        Console.WriteLine($"App running in : {config.Langage}");
    }
}


