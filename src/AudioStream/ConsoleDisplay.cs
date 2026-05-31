namespace AudioStream;

/// <summary>
/// Centralise l'affichage console colore pour garder le demarrage lisible.
/// </summary>
public static class ConsoleDisplay
{
    /// <summary>
    /// Texte brut de la banniere affichee au demarrage.
    /// </summary>
    public const string Banner = """
    _             _ _       ____  _                            
   / \  _   _  __| (_) ___ / ___|| |_ _ __ ___  __ _ _ __ ___  
  / _ \| | | |/ _` | |/ _ \\___ \| __| '__/ _ \/ _` | '_ ` _ \ 
 / ___ \ |_| | (_| | | (_) |___) | |_| | |  __/ (_| | | | | | |
/_/   \_\__,_|\__,_|_|\___/|____/ \__|_|  \___|\__,_|_| |_| |_|
AudioStream
""";

    /// <summary>
    /// Affiche la banniere principale avec une couleur distinctive.
    /// </summary>
    public static void PrintBanner(TextWriter? writer = null)
    {
        WriteLine(Banner, ConsoleColor.Cyan, writer);
    }

    /// <summary>
    /// Ecrit une ligne coloree puis restaure la couleur initiale de la console.
    /// </summary>
    public static void WriteLine(string message, ConsoleColor color, TextWriter? writer = null)
    {
        writer ??= Console.Out;
        var previousColor = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = color;
            writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }

    /// <summary>
    /// Demande une confirmation oui/non a l'utilisateur.
    /// </summary>
    public static bool AskYesNo(string question)
    {
        WriteLine(question, ConsoleColor.Yellow);
        var answer = Console.ReadLine();
        return IsYesAnswer(answer);
    }

    /// <summary>
    /// Interprete les reponses positives francaises et courtes.
    /// </summary>
    public static bool IsYesAnswer(string? answer)
    {
        return string.Equals(answer?.Trim(), "o", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer?.Trim(), "oui", StringComparison.OrdinalIgnoreCase);
    }
}
