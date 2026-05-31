using System.Net;
using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie le choix de l'adresse affichee dans le titre de la console.
/// </summary>
public sealed class ConsoleTitleTests
{
    [Fact]
    public void ResolveConsoleTitleIp_UsesExplicitHost()
    {
        var displayIp = AudioStreamApp.ResolveConsoleTitleIp(
            "127.0.0.1",
            [IPAddress.Parse("192.168.1.33")]);

        Assert.Equal("127.0.0.1", displayIp);
    }

    [Fact]
    public void ResolveConsoleTitleIp_UsesFirstLanAddress_WhenHostBindsAllInterfaces()
    {
        var displayIp = AudioStreamApp.ResolveConsoleTitleIp(
            "0.0.0.0",
            [IPAddress.Parse("192.168.1.33"), IPAddress.Parse("10.0.0.5")]);

        Assert.Equal("192.168.1.33", displayIp);
    }

    [Fact]
    public void ResolveConsoleTitleIp_UsesLocalhost_WhenNoLanAddressExists()
    {
        var displayIp = AudioStreamApp.ResolveConsoleTitleIp("0.0.0.0", []);

        Assert.Equal("localhost", displayIp);
    }
}
