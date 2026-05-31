using System.Net.WebSockets;

namespace AudioStream.Audio;

/// <summary>
/// Adapte un WebSocket navigateur en client audio binaire.
/// </summary>
public sealed class WebSocketAudioClient : IAudioClient
{
    private readonly WebSocket socket;

    /// <summary>
    /// Initialise le client autour du WebSocket accepte par ASP.NET Core.
    /// </summary>
    public WebSocketAudioClient(WebSocket socket)
    {
        this.socket = socket;
    }

    /// <inheritdoc />
    public bool IsOpen => socket.State == WebSocketState.Open;

    /// <inheritdoc />
    public async ValueTask SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        await socket.SendAsync(frame, WebSocketMessageType.Binary, true, cancellationToken);
    }

    /// <summary>
    /// Attend que le navigateur ferme la connexion ou que la requete soit annulee.
    /// </summary>
    public async Task WaitForCloseAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[128];

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.CloseStatus.HasValue)
            {
                await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cancellationToken);
                break;
            }
        }
    }
}
