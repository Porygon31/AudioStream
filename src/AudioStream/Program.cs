using AudioStream;

// Le point d'entree reste volontairement court afin que la logique soit testable.
ConsoleDisplay.PrintBanner();

var app = AudioStreamApp.Build(args);
var options = app.Services.GetRequiredService<AudioStreamOptions>();

AudioStreamApp.SetConsoleTitle(options);
AudioStreamApp.EnsureFirewallPortAllowed(options.Port, new WindowsFirewallPortService(), app.Environment.IsEnvironment("Testing"));
AudioStreamApp.PrintStartupUrls(options);

try
{
    await app.RunAsync();
}
catch (IOException exception) when (AudioStreamApp.IsAddressInUse(exception))
{
    // Kestrel remonte une IOException lorsque le port demande est deja utilise.
    ConsoleDisplay.WriteLine($"Le port {options.Port} est deja utilise. Relancez avec --port <autre-port>.", ConsoleColor.Red, Console.Error);
    Environment.ExitCode = 1;
}

// Cette classe partielle permet aux tests d'integration de demarrer l'application.
public partial class Program;
