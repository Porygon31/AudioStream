using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie le parsing des options de lancement de l'application.
/// </summary>
public sealed class AudioStreamOptionsTests
{
    [Fact]
    public void Parse_UsesDefaults_WhenNoArgumentsAreProvided()
    {
        var options = AudioStreamOptions.Parse(Array.Empty<string>());

        Assert.Equal(AudioStreamOptions.DefaultHost, options.Host);
        Assert.Equal(AudioStreamOptions.DefaultPort, options.Port);
        Assert.Equal(AudioStreamOptions.DefaultSampleRate, options.SampleRate);
    }

    [Fact]
    public void Parse_ReadsSupportedArguments()
    {
        var options = AudioStreamOptions.Parse(["--host", "127.0.0.1", "--port", "5151", "--sample-rate", "44100"]);

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal(5151, options.Port);
        Assert.Equal(44100, options.SampleRate);
    }

    [Fact]
    public void Parse_RejectsInvalidPort()
    {
        var exception = Assert.Throws<ArgumentException>(() => AudioStreamOptions.Parse(["--port", "90000"]));

        Assert.Contains("--port", exception.Message);
    }
}
