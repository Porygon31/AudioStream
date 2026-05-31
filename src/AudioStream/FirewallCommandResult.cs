namespace AudioStream;

/// <summary>
/// Resultat d'une commande systeme liee au pare-feu.
/// </summary>
public sealed record FirewallCommandResult(bool Succeeded, string Output, string Error);
