namespace AudioStream;

/// <summary>
/// Represente les options de lancement de l'application.
/// </summary>
public sealed record AudioStreamOptions(string Host, int Port, int SampleRate)
{
    /// <summary>
    /// Hote par defaut utilise pour exposer le service sur le reseau local.
    /// </summary>
    public const string DefaultHost = "0.0.0.0";

    /// <summary>
    /// Port HTTP par defaut.
    /// </summary>
    public const int DefaultPort = 5100;

    /// <summary>
    /// Frequence d'echantillonnage envoyee au navigateur.
    /// </summary>
    public const int DefaultSampleRate = 48000;

    /// <summary>
    /// Analyse les options de ligne de commande supportees par AudioStream.
    /// </summary>
    public static AudioStreamOptions Parse(IReadOnlyList<string> args)
    {
        var host = DefaultHost;
        var port = DefaultPort;
        var sampleRate = DefaultSampleRate;

        for (var index = 0; index < args.Count; index++)
        {
            var current = args[index];

            switch (current)
            {
                case "--host":
                    host = ReadValue(args, ref index, current);
                    break;

                case "--port":
                    port = ReadInt(args, ref index, current, 1, 65535);
                    break;

                case "--sample-rate":
                    sampleRate = ReadInt(args, ref index, current, 8000, 192000);
                    break;

                default:
                    if (TrySkipHostOption(args, ref index, current))
                    {
                        break;
                    }

                    throw new ArgumentException($"Option inconnue: {current}");
            }
        }

        return new AudioStreamOptions(host, port, sampleRate);
    }

    private static bool TrySkipHostOption(IReadOnlyList<string> args, ref int index, string option)
    {
        // ASP.NET Core et les tests d'integration peuvent ajouter leurs propres arguments.
        if (option.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase)
            || option.StartsWith("--contentRoot=", StringComparison.OrdinalIgnoreCase)
            || option.StartsWith("--applicationName=", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (option is "--environment" or "--contentRoot" or "--applicationName")
        {
            if (index + 1 < args.Count)
            {
                index++;
            }

            return true;
        }

        return false;
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"L'option {option} attend une valeur.");
        }

        index++;
        return args[index];
    }

    private static int ReadInt(IReadOnlyList<string> args, ref int index, string option, int min, int max)
    {
        var value = ReadValue(args, ref index, option);

        if (!int.TryParse(value, out var parsed) || parsed < min || parsed > max)
        {
            throw new ArgumentException($"L'option {option} doit etre un entier entre {min} et {max}.");
        }

        return parsed;
    }
}
