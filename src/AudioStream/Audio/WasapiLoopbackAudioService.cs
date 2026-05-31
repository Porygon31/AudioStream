using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using NAudio.Wave;

namespace AudioStream.Audio;

/// <summary>
/// Service en arriere-plan qui capture le son du PC avec WASAPI loopback.
/// </summary>
public sealed class WasapiLoopbackAudioService : BackgroundService
{
    private readonly AudioBroadcaster broadcaster;
    private readonly AudioStreamOptions options;
    private readonly ILogger<WasapiLoopbackAudioService> logger;
    private readonly Channel<byte[]> frames;

    /// <summary>
    /// Initialise le service de capture et sa file temps reel.
    /// </summary>
    public WasapiLoopbackAudioService(
        AudioBroadcaster broadcaster,
        AudioStreamOptions options,
        ILogger<WasapiLoopbackAudioService> logger)
    {
        this.broadcaster = broadcaster;
        this.options = options;
        this.logger = logger;

        // La diffusion est temps reel: en cas de retard, on jette les anciens paquets.
        frames = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogError("La capture audio systeme est uniquement disponible sous Windows.");
            return;
        }

        using var capture = new WasapiLoopbackCapture();
        var converter = new PcmAudioConverter(capture.WaveFormat, options.SampleRate);

        capture.DataAvailable += (_, eventArgs) =>
        {
            try
            {
                var converted = converter.Convert(eventArgs.Buffer, eventArgs.BytesRecorded);

                if (converted.Length > 0)
                {
                    frames.Writer.TryWrite(converted);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Impossible de convertir un paquet audio.");
            }
        };

        capture.RecordingStopped += (_, eventArgs) =>
        {
            if (eventArgs.Exception is not null)
            {
                logger.LogError(eventArgs.Exception, "La capture audio s'est arretee avec une erreur.");
            }
        };

        try
        {
            capture.StartRecording();
            logger.LogInformation(
                "Capture audio demarree: {Channels} canaux, {SampleRate} Hz, {Bits} bits.",
                capture.WaveFormat.Channels,
                capture.WaveFormat.SampleRate,
                capture.WaveFormat.BitsPerSample);

            await foreach (var frame in frames.Reader.ReadAllAsync(stoppingToken))
            {
                await broadcaster.BroadcastAsync(frame, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // L'annulation est normale lors d'un Ctrl+C ou d'un arret de l'application.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Impossible de capturer le son du PC.");
        }
        finally
        {
            StopCapture(capture);
            frames.Writer.TryComplete();
        }
    }

    private void StopCapture(WasapiLoopbackCapture capture)
    {
        try
        {
            capture.StopRecording();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "La capture etait deja arretee.");
        }
    }
}
