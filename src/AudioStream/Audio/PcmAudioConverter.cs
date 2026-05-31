using NAudio.Wave;

namespace AudioStream.Audio;

/// <summary>
/// Convertit les buffers WASAPI en PCM float32 stereo a frequence stable.
/// </summary>
public sealed class PcmAudioConverter
{
    private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00aa00389b71");
    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");
    private const int TargetChannels = 2;
    private readonly WaveFormat sourceFormat;
    private readonly int targetSampleRate;

    /// <summary>
    /// Cree un convertisseur pour le format source donne par WASAPI.
    /// </summary>
    public PcmAudioConverter(WaveFormat sourceFormat, int targetSampleRate)
    {
        this.sourceFormat = sourceFormat;
        this.targetSampleRate = targetSampleRate;
    }

    /// <summary>
    /// Convertit un paquet audio source vers un tableau de bytes float32 interleaves.
    /// </summary>
    public byte[] Convert(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded <= 0 || sourceFormat.BlockAlign <= 0)
        {
            return Array.Empty<byte>();
        }

        var sourceFrames = bytesRecorded / sourceFormat.BlockAlign;
        if (sourceFrames == 0)
        {
            return Array.Empty<byte>();
        }

        var stereoSamples = DecodeToStereo(buffer, sourceFrames);

        if (sourceFormat.SampleRate != targetSampleRate)
        {
            stereoSamples = ResampleStereo(stereoSamples, sourceFormat.SampleRate, targetSampleRate);
        }

        return FloatSamplesToBytes(stereoSamples);
    }

    private float[] DecodeToStereo(byte[] buffer, int sourceFrames)
    {
        var stereoSamples = new float[sourceFrames * TargetChannels];
        var bytesPerSample = sourceFormat.BitsPerSample / 8;

        for (var frame = 0; frame < sourceFrames; frame++)
        {
            var frameOffset = frame * sourceFormat.BlockAlign;
            var left = ReadSample(buffer, frameOffset, bytesPerSample);

            // Les sources mono sont dupliquees; les sources multicanal utilisent les canaux gauche/droite.
            var right = sourceFormat.Channels > 1
                ? ReadSample(buffer, frameOffset + bytesPerSample, bytesPerSample)
                : left;

            var targetOffset = frame * TargetChannels;
            stereoSamples[targetOffset] = left;
            stereoSamples[targetOffset + 1] = right;
        }

        return stereoSamples;
    }

    private float ReadSample(byte[] buffer, int offset, int bytesPerSample)
    {
        var encoding = ResolveEncoding();

        return encoding switch
        {
            WaveFormatEncoding.IeeeFloat when bytesPerSample == 4 => BitConverter.ToSingle(buffer, offset),
            WaveFormatEncoding.Pcm when bytesPerSample == 1 => (buffer[offset] - 128) / 128f,
            WaveFormatEncoding.Pcm when bytesPerSample == 2 => BitConverter.ToInt16(buffer, offset) / 32768f,
            WaveFormatEncoding.Pcm when bytesPerSample == 3 => ReadInt24(buffer, offset) / 8388608f,
            WaveFormatEncoding.Pcm when bytesPerSample == 4 => BitConverter.ToInt32(buffer, offset) / 2147483648f,
            _ => throw new NotSupportedException($"Format audio non supporte: {sourceFormat.Encoding}, {sourceFormat.BitsPerSample} bits.")
        };
    }

    private WaveFormatEncoding ResolveEncoding()
    {
        if (sourceFormat.Encoding != WaveFormatEncoding.Extensible || sourceFormat is not WaveFormatExtensible extensible)
        {
            return sourceFormat.Encoding;
        }

        // WASAPI peut exposer un conteneur extensible; le sous-format donne le vrai codage.
        if (extensible.SubFormat == PcmSubFormat)
        {
            return WaveFormatEncoding.Pcm;
        }

        if (extensible.SubFormat == IeeeFloatSubFormat)
        {
            return WaveFormatEncoding.IeeeFloat;
        }

        return sourceFormat.Encoding;
    }

    private static int ReadInt24(byte[] buffer, int offset)
    {
        var value = buffer[offset] | buffer[offset + 1] << 8 | buffer[offset + 2] << 16;

        // Les entiers PCM 24 bits sont signes; le bit 23 doit etre propage.
        if ((value & 0x800000) != 0)
        {
            value |= unchecked((int)0xFF000000);
        }

        return value;
    }

    private static float[] ResampleStereo(float[] source, int sourceRate, int targetRate)
    {
        var sourceFrames = source.Length / TargetChannels;
        var targetFrames = Math.Max(1, (int)Math.Round(sourceFrames * (double)targetRate / sourceRate));
        var target = new float[targetFrames * TargetChannels];

        for (var frame = 0; frame < targetFrames; frame++)
        {
            var sourcePosition = frame * (double)sourceRate / targetRate;
            var lowerFrame = Math.Min((int)Math.Floor(sourcePosition), sourceFrames - 1);
            var upperFrame = Math.Min(lowerFrame + 1, sourceFrames - 1);
            var fraction = (float)(sourcePosition - lowerFrame);

            for (var channel = 0; channel < TargetChannels; channel++)
            {
                var lower = source[(lowerFrame * TargetChannels) + channel];
                var upper = source[(upperFrame * TargetChannels) + channel];
                target[(frame * TargetChannels) + channel] = lower + (upper - lower) * fraction;
            }
        }

        return target;
    }

    private static byte[] FloatSamplesToBytes(float[] samples)
    {
        var bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
