namespace AudioStream;

/// <summary>
/// Contrat de verification et d'ouverture d'un port dans le pare-feu Windows.
/// </summary>
public interface IWindowsFirewallPortService
{
    /// <summary>
    /// Indique si le service peut agir sur le systeme courant.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Verifie si le port TCP entrant est deja autorise.
    /// </summary>
    bool IsPortAllowed(int port);

    /// <summary>
    /// Tente d'ajouter une regle entrante pour le port TCP.
    /// </summary>
    FirewallCommandResult OpenPort(int port);

    /// <summary>
    /// Construit la commande d'ouverture manuelle du port.
    /// </summary>
    string BuildOpenPortCommand(int port);
}
