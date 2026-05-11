using EasySaveProject.Core.Localization;

public class InputForm
{
    private readonly LocalizationService _loc;

    public InputForm(LocalizationService loc)
    {
        _loc = loc;
    }

    public string AskRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
                return input;

            Console.WriteLine(_loc.T("Form.EmptyError"));
        }
    }
}