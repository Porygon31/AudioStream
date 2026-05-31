using System.Diagnostics;

namespace AudioStream;

/// <summary>
/// Interroge et modifie le pare-feu Windows pour le port TCP AudioStream.
/// </summary>
public sealed class WindowsFirewallPortService : IWindowsFirewallPortService
{
    private readonly ICommandRunner commandRunner;

    /// <summary>
    /// Initialise le service avec le runner de processus par defaut.
    /// </summary>
    public WindowsFirewallPortService()
        : this(new ProcessCommandRunner())
    {
    }

    /// <summary>
    /// Initialise le service avec un runner injectable pour les tests.
    /// </summary>
    public WindowsFirewallPortService(ICommandRunner commandRunner)
    {
        this.commandRunner = commandRunner;
    }

    /// <inheritdoc />
    public bool IsSupported => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsPortAllowed(int port)
    {
        var result = commandRunner.Run("netsh", BuildCheckPortArguments(port));
        return result.Succeeded;
    }

    /// <inheritdoc />
    public FirewallCommandResult OpenPort(int port)
    {
        return commandRunner.Run("netsh", BuildOpenPortArguments(port));
    }

    /// <inheritdoc />
    public string BuildOpenPortCommand(int port)
    {
        return $"netsh {BuildOpenPortArguments(port)}";
    }

    private static string BuildCheckPortArguments(int port)
    {
        // La verification cible la regle creee par AudioStream pour rester rapide au demarrage.
        return $"advfirewall firewall show rule name=\"AudioStream TCP {port}\"";
    }

    private static string BuildOpenPortArguments(int port)
    {
        return $"advfirewall firewall add rule name=\"AudioStream TCP {port}\" dir=in action=allow protocol=TCP localport={port}";
    }
}

/// <summary>
/// Execute une commande systeme et retourne sa sortie.
/// </summary>
public interface ICommandRunner
{
    /// <summary>
    /// Lance le fichier executable avec les arguments donnes.
    /// </summary>
    FirewallCommandResult Run(string fileName, string arguments);
}

/// <summary>
/// Implementation reelle du runner basee sur System.Diagnostics.Process.
/// </summary>
public sealed class ProcessCommandRunner : ICommandRunner
{
    /// <inheritdoc />
    public FirewallCommandResult Run(string fileName, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new FirewallCommandResult(process.ExitCode == 0, output, error);
    }
}
