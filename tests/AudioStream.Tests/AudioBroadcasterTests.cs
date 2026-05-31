using AudioStream.Audio;
using Microsoft.Extensions.Logging.Abstractions;

namespace AudioStream.Tests;

/// <summary>
/// Couvre la diffusion des paquets audio vers les clients connectes.
/// </summary>
public sealed class AudioBroadcasterTests
{
    [Fact]
    public async Task BroadcastAsync_SendsFrameToAllOpenClients()
    {
        var broadcaster = new AudioBroadcaster(NullLogger<AudioBroadcaster>.Instance);
        var firstClient = new FakeAudioClient();
        var secondClient = new FakeAudioClient();
        var frame = new byte[] { 1, 2, 3, 4 };

        broadcaster.AddClient(firstClient);
        broadcaster.AddClient(secondClient);

        await broadcaster.BroadcastAsync(frame, CancellationToken.None);

        Assert.Single(firstClient.SentFrames);
        Assert.Single(secondClient.SentFrames);
        Assert.Equal(frame, firstClient.SentFrames[0]);
        Assert.Equal(frame, secondClient.SentFrames[0]);
    }

    [Fact]
    public async Task BroadcastAsync_RemovesClosedClients()
    {
        var broadcaster = new AudioBroadcaster(NullLogger<AudioBroadcaster>.Instance);
        var closedClient = new FakeAudioClient { IsOpen = false };

        broadcaster.AddClient(closedClient);

        await broadcaster.BroadcastAsync(new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(0, broadcaster.ClientCount);
        Assert.Empty(closedClient.SentFrames);
    }

    [Fact]
    public async Task BroadcastAsync_RemovesClientsThatThrowDuringSend()
    {
        var broadcaster = new AudioBroadcaster(NullLogger<AudioBroadcaster>.Instance);
        var failingClient = new FakeAudioClient { ThrowOnSend = true };

        broadcaster.AddClient(failingClient);

        await broadcaster.BroadcastAsync(new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(0, broadcaster.ClientCount);
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
}
