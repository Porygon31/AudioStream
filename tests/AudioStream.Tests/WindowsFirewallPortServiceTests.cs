using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie la construction et l'interpretation des commandes pare-feu.
/// </summary>
public sealed class WindowsFirewallPortServiceTests
{
    [Fact]
    public void BuildOpenPortCommand_UsesExpectedNetshRule()
    {
        var service = new WindowsFirewallPortService(new FakeCommandRunner(new FirewallCommandResult(true, "", "")));

        var command = service.BuildOpenPortCommand(5100);

        Assert.Equal(
            "netsh advfirewall firewall add rule name=\"AudioStream TCP 5100\" dir=in action=allow protocol=TCP localport=5100",
            command);
    }

    [Fact]
    public void IsPortAllowed_ReturnsTrue_WhenFirewallRuleExists()
    {
        var runner = new FakeCommandRunner(new FirewallCommandResult(true, "Nom de la regle: AudioStream TCP 5100", ""));
        var service = new WindowsFirewallPortService(runner);

        var isAllowed = service.IsPortAllowed(5100);

        Assert.True(isAllowed);
        Assert.Equal("netsh", runner.LastFileName);
        Assert.Contains("show rule", runner.LastArguments);
        Assert.Contains("AudioStream TCP 5100", runner.LastArguments);
    }

    [Fact]
    public void IsPortAllowed_ReturnsFalse_WhenFirewallRuleDoesNotExist()
    {
        var service = new WindowsFirewallPortService(new FakeCommandRunner(new FirewallCommandResult(false, "", "Aucune regle.")));

        Assert.False(service.IsPortAllowed(5100));
    }

    [Fact]
    public void OpenPort_RunsNetshCommand()
    {
        var runner = new FakeCommandRunner(new FirewallCommandResult(true, "Ok.", ""));
        var service = new WindowsFirewallPortService(runner);

        var result = service.OpenPort(5100);

        Assert.True(result.Succeeded);
        Assert.Equal("netsh", runner.LastFileName);
        Assert.Contains("AudioStream TCP 5100", runner.LastArguments);
        Assert.Contains("localport=5100", runner.LastArguments);
    }

    /// <summary>
    /// Runner de commande simulant les reponses systeme du pare-feu.
    /// </summary>
    private sealed class FakeCommandRunner : ICommandRunner
    {
        private readonly FirewallCommandResult result;

        public FakeCommandRunner(FirewallCommandResult result)
        {
            this.result = result;
        }

        public string LastFileName { get; private set; } = string.Empty;

        public string LastArguments { get; private set; } = string.Empty;

        public FirewallCommandResult Run(string fileName, string arguments)
        {
            LastFileName = fileName;
            LastArguments = arguments;
            return result;
        }
    }
}
