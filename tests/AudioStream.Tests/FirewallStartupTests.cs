using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie la coordination de demarrage autour du pare-feu.
/// </summary>
public sealed class FirewallStartupTests
{
    [Fact]
    public void EnsureFirewallPortAllowed_DoesNotOpenPort_WhenAlreadyAllowed()
    {
        var firewall = new FakeFirewallPortService(isAllowed: true);

        AudioStreamApp.EnsureFirewallPortAllowed(5100, firewall, skipPrompt: false);

        Assert.False(firewall.OpenPortWasCalled);
    }

    [Fact]
    public void EnsureFirewallPortAllowed_DoesNothing_WhenSkippedForTests()
    {
        var firewall = new FakeFirewallPortService(isAllowed: false);

        AudioStreamApp.EnsureFirewallPortAllowed(5100, firewall, skipPrompt: true);

        Assert.False(firewall.IsPortAllowedWasCalled);
        Assert.False(firewall.OpenPortWasCalled);
    }

    /// <summary>
    /// Faux service pare-feu utilise pour verifier le flux de demarrage.
    /// </summary>
    private sealed class FakeFirewallPortService : IWindowsFirewallPortService
    {
        private readonly bool isAllowed;

        public FakeFirewallPortService(bool isAllowed)
        {
            this.isAllowed = isAllowed;
        }

        public bool IsSupported => true;

        public bool IsPortAllowedWasCalled { get; private set; }

        public bool OpenPortWasCalled { get; private set; }

        public bool IsPortAllowed(int port)
        {
            IsPortAllowedWasCalled = true;
            return isAllowed;
        }

        public FirewallCommandResult OpenPort(int port)
        {
            OpenPortWasCalled = true;
            return new FirewallCommandResult(true, "", "");
        }

        public string BuildOpenPortCommand(int port)
        {
            return $"open {port}";
        }
    }
}
