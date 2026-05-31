using System.Collections.Concurrent;

namespace AudioStream.Audio;

/// <summary>
/// Maintient la liste des clients connectes et leur diffuse les paquets audio.
/// </summary>
public sealed class AudioBroadcaster
{
    private readonly ConcurrentDictionary<Guid, IAudioClient> clients = new();
    private readonly ILogger<AudioBroadcaster> logger;

    /// <summary>
    /// Initialise le diffuseur avec un logger ASP.NET Core.
    /// </summary>
    public AudioBroadcaster(ILogger<AudioBroadcaster> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Nombre de clients actuellement connus du diffuseur.
    /// </summary>
    public int ClientCount => clients.Count;

    /// <summary>
    /// Ajoute un client et retourne son identifiant interne.
    /// </summary>
    public Guid AddClient(IAudioClient client)
    {
        var id = Guid.NewGuid();
        clients[id] = client;
        logger.LogInformation("Client audio connecte: {ClientId}", id);
        return id;
    }

    /// <summary>
    /// Retire un client de la diffusion.
    /// </summary>
    public void RemoveClient(Guid clientId)
    {
        if (clients.TryRemove(clientId, out _))
        {
            logger.LogInformation("Client audio deconnecte: {ClientId}", clientId);
        }
    }

    /// <summary>
    /// Diffuse un paquet a tous les clients ouverts et retire les clients en erreur.
    /// </summary>
    public async ValueTask BroadcastAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        foreach (var (clientId, client) in clients.ToArray())
        {
            if (!client.IsOpen)
            {
                RemoveClient(clientId);
                continue;
            }

            try
            {
                await client.SendAsync(frame, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Client audio retire apres une erreur d'envoi: {ClientId}", clientId);
                RemoveClient(clientId);
            }
        }
    }
}
