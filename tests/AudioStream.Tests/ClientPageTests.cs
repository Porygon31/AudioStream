using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie que la page client contient les mecanismes de lecture attendus.
/// </summary>
public sealed class ClientPageTests
{
    [Fact]
    public void Render_IncludesAudioWorkletFallbackForHttpLan()
    {
        var html = ClientPage.Render(48000);

        Assert.Contains("audioContext.audioWorklet", html);
        Assert.Contains("createScriptProcessor", html);
        Assert.Contains("Fallback utile sur HTTP LAN", html);
    }

    [Fact]
    public void Render_IncludesPauseAndResumeControls()
    {
        var html = ClientPage.Render(48000);

        Assert.Contains("Pause", html);
        Assert.Contains("Reprendre", html);
        Assert.Contains("audioContext.resume()", html);
    }

    [Fact]
    public void Render_StopsStreamAndClearsBuffersWhenPaused()
    {
        var html = ClientPage.Render(48000);

        Assert.DoesNotContain("audioContext.suspend()", html);
        Assert.Contains("socket.close(1000, \"Pause\")", html);
        Assert.Contains("type: \"clear\"", html);
        Assert.Contains("queueState.queue.length = 0", html);
    }

    [Fact]
    public void Render_InjectsConfiguredSampleRate()
    {
        var html = ClientPage.Render(44100);

        Assert.Contains("const STREAM_SAMPLE_RATE = 44100;", html);
    }
}
