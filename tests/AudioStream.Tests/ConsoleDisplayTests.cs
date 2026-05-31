using AudioStream;

namespace AudioStream.Tests;

/// <summary>
/// Verifie les helpers d'affichage console colore.
/// </summary>
public sealed class ConsoleDisplayTests
{
    [Fact]
    public void Banner_IsNotEmptyAndContainsAudioStream()
    {
        Assert.False(string.IsNullOrWhiteSpace(ConsoleDisplay.Banner));
        Assert.Contains("AudioStream", ConsoleDisplay.Banner);
    }

    [Fact]
    public void WriteLine_RestoresInitialConsoleColor()
    {
        var initialColor = Console.ForegroundColor;
        using var writer = new StringWriter();

        ConsoleDisplay.WriteLine("Message colore", ConsoleColor.Cyan, writer);

        Assert.Equal(initialColor, Console.ForegroundColor);
        Assert.Contains("Message colore", writer.ToString());
    }

    [Theory]
    [InlineData("o", true)]
    [InlineData("O", true)]
    [InlineData("oui", true)]
    [InlineData("Oui", true)]
    [InlineData("n", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsYesAnswer_ParsesFrenchAnswers(string? answer, bool expected)
    {
        Assert.Equal(expected, ConsoleDisplay.IsYesAnswer(answer));
    }
}
