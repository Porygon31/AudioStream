using System.Net;
using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie le format des adresses client affichees dans les logs console.
/// </summary>
public sealed class RemoteEndpointTests
{
    [Fact]
    public void FormatRemoteEndpoint_ReturnsUnknown_WhenIpIsMissing()
    {
        var endpoint = AudioStreamApp.FormatRemoteEndpoint(null, 54321);

        Assert.Equal("IP inconnue", endpoint);
    }

    [Fact]
    public void FormatRemoteEndpoint_ReturnsIpv4WithPort()
    {
        var endpoint = AudioStreamApp.FormatRemoteEndpoint(IPAddress.Parse("192.168.1.42"), 54321);

        Assert.Equal("192.168.1.42:54321", endpoint);
    }

    [Fact]
    public void FormatRemoteEndpoint_MapsIpv4MappedIpv6ToIpv4()
    {
        var endpoint = AudioStreamApp.FormatRemoteEndpoint(IPAddress.Parse("::ffff:192.168.1.42"), 54321);

        Assert.Equal("192.168.1.42:54321", endpoint);
    }

    [Fact]
    public void FormatRemoteEndpoint_OmitsPort_WhenPortIsNotAvailable()
    {
        var endpoint = AudioStreamApp.FormatRemoteEndpoint(IPAddress.Parse("192.168.1.42"), 0);

        Assert.Equal("192.168.1.42", endpoint);
    }
}
