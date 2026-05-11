namespace EasySaveProject.Core.Services;

// Permet à l'utilisateur de configurer des variables d'environnement
public class EnvConfigService
{
    private const string VAR_SOURCE = "EASYSAVE_SOURCE";
    private const string VAR_TARGET = "EASYSAVE_TARGET";
    private const string VAR_LOGS = "EASYSAVE_LOG_DIR";

    public string? GetSource() => Environment.GetEnvironmentVariable(VAR_SOURCE);
    public string? GetTarget() => Environment.GetEnvironmentVariable(VAR_TARGET);
    public string? GetLogDir() => Environment.GetEnvironmentVariable(VAR_LOGS);


    // Demande à l'utilisateur s'il veut configurer les variables,
    public void ProposerConfiguration()
    {
        Console.WriteLine("\n Voulez-vous configurer les variables d'environnement ? (o/n)");
        string? reponse = Console.ReadLine()?.Trim().ToLower();

        if (reponse != "o") return;

        ConfigurerVariable(VAR_SOURCE, "Chemin source par défaut");
        ConfigurerVariable(VAR_TARGET, "Chemin cible par défaut");
        ConfigurerVariable(VAR_LOGS, "Dossier des logs");

        Console.WriteLine("\n Variables configurées pour cette session.");
        Console.WriteLine("   Pour les rendre permanentes, ajoutez-les dans vos variables système Windows.");
    }

    // Configure une variable pour la session en cours.
    private void ConfigurerVariable(string nom, string description)
    {
        string? actuelle = Environment.GetEnvironmentVariable(nom);

        Console.Write($"\n  {description}");
        if (actuelle != null)
            Console.Write($" (actuelle : {actuelle})");
        Console.Write(" : ");

        string? valeur = Console.ReadLine()?.Trim();

        // Si l'utilisateur entre quelque chose, on met à jour
        if (!string.IsNullOrEmpty(valeur))
        {
            Environment.SetEnvironmentVariable(nom, valeur, EnvironmentVariableTarget.Process);
            Console.WriteLine($"{nom} = {valeur}");
        }
    }
}