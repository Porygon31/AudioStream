using AudioStream.Audio;
using Microsoft.Extensions.Logging;

namespace AudioStream.Tests;

/// <summary>
/// Couvre la diffusion des paquets audio vers les clients connectes.
/// </summary>
public sealed class AudioBroadcasterTests
{
    [Fact]
    public async Task BroadcastAsync_SendsFrameToAllOpenClients()
    {
        var broadcaster = new AudioBroadcaster(new TestLogger<AudioBroadcaster>());
        var firstClient = new FakeAudioClient();
        var secondClient = new FakeAudioClient();
        var frame = new byte[] { 1, 2, 3, 4 };

        broadcaster.AddClient(firstClient, "192.168.1.42:54321");
        broadcaster.AddClient(secondClient, "192.168.1.43:54322");

        await broadcaster.BroadcastAsync(frame, CancellationToken.None);

        Assert.Single(firstClient.SentFrames);
        Assert.Single(secondClient.SentFrames);
        Assert.Equal(frame, firstClient.SentFrames[0]);
        Assert.Equal(frame, secondClient.SentFrames[0]);
    }

    [Fact]
    public async Task BroadcastAsync_RemovesClosedClients()
    {
        var broadcaster = new AudioBroadcaster(new TestLogger<AudioBroadcaster>());
        var closedClient = new FakeAudioClient { IsOpen = false };

        broadcaster.AddClient(closedClient, "192.168.1.42:54321");

        await broadcaster.BroadcastAsync(new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(0, broadcaster.ClientCount);
        Assert.Empty(closedClient.SentFrames);
    }

    [Fact]
    public async Task BroadcastAsync_RemovesClientsThatThrowDuringSend()
    {
        var broadcaster = new AudioBroadcaster(new TestLogger<AudioBroadcaster>());
        var failingClient = new FakeAudioClient { ThrowOnSend = true };

        broadcaster.AddClient(failingClient, "192.168.1.42:54321");

        await broadcaster.BroadcastAsync(new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(0, broadcaster.ClientCount);
    }

    [Fact]
    public void AddAndRemoveClient_LogsRemoteEndpoint()
    {
        var logger = new TestLogger<AudioBroadcaster>();
        var broadcaster = new AudioBroadcaster(logger);
        var client = new FakeAudioClient();

        var clientId = broadcaster.AddClient(client, "192.168.1.42:54321");
        broadcaster.RemoveClient(clientId);

        Assert.Contains(logger.Messages, message => message.Contains("Client audio connecte depuis 192.168.1.42:54321"));
        Assert.Contains(logger.Messages, message => message.Contains("Client audio deconnecte depuis 192.168.1.42:54321"));
    }

    /// <summary>
    /// Faux client audio utilise pour verifier le comportement du diffuseur.
    /// </summary>
    private sealed class FakeAudioClient : IAudioClient
    {
        public bool IsOpen { get; init; } = true;

        public bool ThrowOnSend { get; init; }

        public List<byte[]> SentFrames { get; } = [];

        public ValueTask SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Erreur simulee.");
            }

            SentFrames.Add(frame.ToArray());
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Logger minimal qui garde les messages pour les assertions unitaires.
    /// </summary>
    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
