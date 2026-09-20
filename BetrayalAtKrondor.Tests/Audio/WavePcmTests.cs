namespace BetrayalAtKrondor.Tests.Audio;

using System;
using System.IO;
using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// Reading back the RIFF wrapper the extractor writes around a digitised sample.
/// </summary>
/// <remarks>
/// The bytes under test are built the way <c>AudioParser.ParseWave</c> builds them — mono, 8-bit
/// unsigned PCM — because that is the only producer this ever reads (TASK-601).
/// </remarks>
public class WavePcmTests {
    private static byte[] Wav(byte[] data, int sampleRate = 11025, int bits = 8, int channels = 1) {
        using var output = new MemoryStream();
        using var w = new BinaryWriter(output);
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + data.Length);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);
        w.Write((ushort)1);
        w.Write((ushort)channels);
        w.Write(sampleRate);
        w.Write(sampleRate * channels * bits / 8);
        w.Write((ushort)(channels * bits / 8));
        w.Write((ushort)bits);
        w.Write("data"u8.ToArray());
        w.Write(data.Length);
        w.Write(data);
        w.Flush();

        return output.ToArray();
    }

    [Fact]
    public void TheSampleRateAndLengthSurviveTheRoundTrip() {
        WavePcm.Pcm? pcm = WavePcm.Decode(Wav(new byte[] { 128, 200, 60, 128 }, sampleRate: 8000));

        Assert.NotNull(pcm);
        Assert.Equal(8000, pcm!.Value.SampleRate);
        Assert.Equal(1, pcm.Value.Channels);
        Assert.Equal(4, pcm.Value.Samples.Length);
    }

    [Fact]
    public void EightBitIsUNSIGNED_SoMidScaleIsSilence() {
        // The asymmetry that matters: 8-bit WAV centres on 128, 16-bit on 0. Reading 8-bit as
        // signed inverts the waveform around a DC offset and sounds like clipping, not silence.
        WavePcm.Pcm? pcm = WavePcm.Decode(Wav(new byte[] { 128, 255, 0 }));

        Assert.Equal(0f, pcm!.Value.Samples[0]);
        Assert.True(pcm.Value.Samples[1] > 0.9f, "full scale high should be near +1");
        Assert.Equal(-1f, pcm.Value.Samples[2]);
    }

    [Fact]
    public void AChunkWalkFindsDataEvenWithSomethingBeforeIt() {
        // The reason this walks chunks instead of skipping 44 bytes: an extra chunk shifts the
        // payload, and a fixed offset would decode noise without saying anything was wrong.
        byte[] normal = Wav(new byte[] { 128, 255 });
        var padded = new MemoryStream();
        padded.Write(normal, 0, 12);                       // RIFF....WAVE
        padded.Write("LIST"u8.ToArray(), 0, 4);
        padded.Write(BitConverter.GetBytes(4), 0, 4);
        padded.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
        padded.Write(normal, 12, normal.Length - 12);      // then fmt + data

        WavePcm.Pcm? pcm = WavePcm.Decode(padded.ToArray());

        Assert.NotNull(pcm);
        Assert.Equal(2, pcm!.Value.Samples.Length);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void BytesThatAreNotAWavAnswerNullRatherThanThrowing(byte[] junk) {
        // A cue that cannot be decoded should cost the player a sound, not throw inside whatever
        // raised it.
        Assert.Null(WavePcm.Decode(junk));
    }

    [Fact]
    public void ARiffThatIsNotWaveIsRefused() {
        byte[] wav = Wav(new byte[] { 128 });
        wav[8] = (byte)'A';   // WAVE -> AAVE

        Assert.Null(WavePcm.Decode(wav));
    }
}
