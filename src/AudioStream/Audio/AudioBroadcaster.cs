using System.Collections.Concurrent;

namespace AudioStream.Audio;

/// <summary>
/// Maintient la liste des clients connectes et leur diffuse les paquets audio.
/// </summary>
public sealed class AudioBroadcaster
{
    private readonly ConcurrentDictionary<Guid, ConnectedAudioClient> clients = new();
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
    public Guid AddClient(IAudioClient client, string remoteEndpoint)
    {
        var id = Guid.NewGuid();
        clients[id] = new ConnectedAudioClient(client, remoteEndpoint);
        logger.LogInformation("Client audio connecte depuis {RemoteEndpoint}", remoteEndpoint);
        return id;
    }

    /// <summary>
    /// Retire un client de la diffusion.
    /// </summary>
    public void RemoveClient(Guid clientId)
    {
        if (clients.TryRemove(clientId, out var connectedClient))
        {
            logger.LogInformation("Client audio deconnecte depuis {RemoteEndpoint}", connectedClient.RemoteEndpoint);
        }
    }

    /// <summary>
    /// Diffuse un paquet a tous les clients ouverts et retire les clients en erreur.
    /// </summary>
    public async ValueTask BroadcastAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        foreach (var (clientId, connectedClient) in clients.ToArray())
        {
            var client = connectedClient.Client;

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
                logger.LogWarning(exception, "Client audio retire apres une erreur d'envoi depuis {RemoteEndpoint}", connectedClient.RemoteEndpoint);
                RemoveClient(clientId);
            }
        }
    }

    private sealed record ConnectedAudioClient(IAudioClient Client, string RemoteEndpoint);
}
