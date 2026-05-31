namespace AudioStream.Audio;

/// <summary>
/// Abstraction minimale d'un client capable de recevoir des paquets audio.
/// </summary>
public interface IAudioClient
{
    /// <summary>
    /// Indique si le client peut encore recevoir des donnees.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Envoie un paquet PCM float32 stereo au client.
    /// </summary>
    ValueTask SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken);
}
