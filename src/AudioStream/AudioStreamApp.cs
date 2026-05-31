using System.Net;
using AudioStream.Audio;
using Microsoft.AspNetCore.Http;

namespace AudioStream;

/// <summary>
/// Construit l'application web et centralise les points d'entree HTTP.
/// </summary>
public static class AudioStreamApp
{
    /// <summary>
    /// Cree l'application ASP.NET Core avec les services de capture et de diffusion.
    /// </summary>
    public static WebApplication Build(string[] args)
    {
        var options = AudioStreamOptions.Parse(args);
        var builder = WebApplication.CreateBuilder(args);

        // L'application ecoute sur toutes les interfaces par defaut pour etre visible sur le LAN.
        builder.WebHost.UseUrls($"http://{options.Host}:{options.Port}");

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<AudioBroadcaster>();

        // Les tests d'integration utilisent l'environnement Testing et ne doivent pas ouvrir WASAPI.
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<WasapiLoopbackAudioService>();
        }

        var app = builder.Build();

        app.UseWebSockets();
        app.MapGet("/", (AudioStreamOptions currentOptions) =>
            Results.Content(ClientPage.Render(currentOptions.SampleRate), "text/html; charset=utf-8"));

        app.MapGet("/health", (AudioStreamOptions currentOptions, AudioBroadcaster broadcaster) =>
            Results.Json(new
            {
                status = "ok",
                sampleRate = currentOptions.SampleRate,
                clients = broadcaster.ClientCount
            }));

        app.Map("/audio", HandleAudioWebSocketAsync);

        return app;
    }

    /// <summary>
    /// Affiche les URLs que les appareils du reseau local peuvent ouvrir.
    /// </summary>
    public static void PrintStartupUrls(AudioStreamOptions options)
    {
        ConsoleDisplay.WriteLine("AudioStream demarre.", ConsoleColor.Green);
        ConsoleDisplay.WriteLine($"URL locale: http://localhost:{options.Port}", ConsoleColor.Cyan);

        foreach (var address in GetLanAddresses())
        {
            ConsoleDisplay.WriteLine($"URL LAN:    http://{address}:{options.Port}", ConsoleColor.Cyan);
        }

        ConsoleDisplay.WriteLine("Si un autre appareil ne peut pas se connecter, verifiez le pare-feu Windows.", ConsoleColor.Yellow);
    }

    /// <summary>
    /// Verifie que le pare-feu Windows autorise le port et propose de l'ouvrir si besoin.
    /// </summary>
    public static void EnsureFirewallPortAllowed(int port, IWindowsFirewallPortService firewallPortService, bool skipPrompt)
    {
        if (skipPrompt)
        {
            return;
        }

        if (!firewallPortService.IsSupported)
        {
            ConsoleDisplay.WriteLine("Verification du pare-feu ignoree: systeme non Windows.", ConsoleColor.Yellow);
            return;
        }

        if (firewallPortService.IsPortAllowed(port))
        {
            ConsoleDisplay.WriteLine($"Pare-feu Windows: le port TCP {port} est deja autorise.", ConsoleColor.Green);
            return;
        }

        var shouldOpenPort = ConsoleDisplay.AskYesNo($"Le port {port} n'est pas autorise dans le pare-feu Windows. Voulez-vous l'ouvrir ? (o/N)");

        if (!shouldOpenPort)
        {
            ConsoleDisplay.WriteLine("Port non ouvert automatiquement. Les appareils du LAN peuvent etre bloques.", ConsoleColor.Yellow);
            return;
        }

        var result = firewallPortService.OpenPort(port);

        if (result.Succeeded)
        {
            ConsoleDisplay.WriteLine($"Pare-feu Windows: le port TCP {port} a ete autorise.", ConsoleColor.Green);
            return;
        }

        ConsoleDisplay.WriteLine("Impossible d'ouvrir le port automatiquement. Lancez la commande suivante en administrateur:", ConsoleColor.Red);
        ConsoleDisplay.WriteLine(firewallPortService.BuildOpenPortCommand(port), ConsoleColor.Yellow);
    }

    /// <summary>
    /// Definit le titre de la console avec l'IP et le port utiles pour l'utilisateur.
    /// </summary>
    public static void SetConsoleTitle(AudioStreamOptions options)
    {
        var displayIp = ResolveConsoleTitleIp(options.Host, GetLanAddresses());
        Console.Title = $"Audio Stream - IP: \"{displayIp}\" Port: \"{options.Port}\"";
    }

    /// <summary>
    /// Choisit l'adresse a afficher dans le titre de la console.
    /// </summary>
    public static string ResolveConsoleTitleIp(string configuredHost, IEnumerable<IPAddress> lanAddresses)
    {
        if (!string.Equals(configuredHost, AudioStreamOptions.DefaultHost, StringComparison.OrdinalIgnoreCase))
        {
            return configuredHost;
        }

        return lanAddresses.FirstOrDefault()?.ToString() ?? "localhost";
    }

    /// <summary>
    /// Detecte si l'erreur de demarrage correspond a un port deja utilise.
    /// </summary>
    public static bool IsAddressInUse(IOException exception)
    {
        return exception.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("adresse", StringComparison.OrdinalIgnoreCase)
            || exception.InnerException is not null && IsAddressInUse(exception.InnerException);
    }

    private static bool IsAddressInUse(Exception exception)
    {
        return exception.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("adresse", StringComparison.OrdinalIgnoreCase)
            || exception.InnerException is not null && IsAddressInUse(exception.InnerException);
    }

    private static async Task HandleAudioWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Cette route accepte uniquement les WebSockets.");
            return;
        }

        var broadcaster = context.RequestServices.GetRequiredService<AudioBroadcaster>();
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var client = new WebSocketAudioClient(socket);
        var remoteEndpoint = FormatRemoteEndpoint(context.Connection.RemoteIpAddress, context.Connection.RemotePort);
        var clientId = broadcaster.AddClient(client, remoteEndpoint);

        try
        {
            // La boucle de reception sert a detecter la fermeture cote navigateur.
            await client.WaitForCloseAsync(context.RequestAborted);
        }
        finally
        {
            broadcaster.RemoveClient(clientId);
        }
    }

    private static IEnumerable<IPAddress> GetLanAddresses()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        return host.AddressList
            .Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Where(address => !IPAddress.IsLoopback(address));
    }

    /// <summary>
    /// Formate l'adresse reseau distante pour les logs de connexion client.
    /// </summary>
    public static string FormatRemoteEndpoint(IPAddress? remoteIpAddress, int remotePort)
    {
        if (remoteIpAddress is null)
        {
            return "IP inconnue";
        }

        // Les connexions locales peuvent arriver sous forme IPv6 mappee; l'IPv4 est plus lisible.
        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        if (remotePort <= 0)
        {
            return remoteIpAddress.ToString();
        }

        if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return $"[{remoteIpAddress}]:{remotePort}";
        }

        return $"{remoteIpAddress}:{remotePort}";
    }
}
