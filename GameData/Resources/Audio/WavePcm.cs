namespace GameData.Resources.Audio;

using System;

/// <summary>
/// Reads the RIFF WAV a digitised sample is stored as, back into samples anything can play.
/// </summary>
/// <remarks>
/// <b>The WAV is ours, not the game's.</b> KRONDOR.001 stores a sample as a sample rate, a length
/// and raw bytes; <c>AudioParser.ParseWave</c> wraps those in a RIFF header so the extracted file
/// is playable on a desktop. This reads that wrapper back off, so the runtime can hand the samples
/// to an engine without going through a file.
///
/// <para><b>The shape is fixed by the writer</b>: mono, 8-bit UNSIGNED PCM, with the sample rate
/// carried from the resource. The chunk walk below is still a real walk rather than a 44-byte skip,
/// because a fixed offset would fail silently and confusingly the day that writer changes.</para>
///
/// <para>Engine-independent on purpose: it answers in a sample rate and normalised floats, which is
/// what every audio API takes, rather than in any engine's clip type.</para>
/// </remarks>
public static class WavePcm {
    /// <summary>What a decoded sample carries.</summary>
    public readonly struct Pcm {
        public Pcm(int sampleRate, int channels, float[] samples) {
            SampleRate = sampleRate;
            Channels = channels;
            Samples = samples;
        }

        public int SampleRate { get; }

        public int Channels { get; }

        /// <summary>Interleaved samples, normalised to -1..1.</summary>
        public float[] Samples { get; }
    }

    /// <summary>
    /// Decode a RIFF WAV, or null when the bytes are not one we can read.
    /// </summary>
    /// <remarks>
    /// Returns null rather than throwing: a sound that cannot be decoded should cost the player a
    /// missing cue, not an exception in the middle of whatever raised it.
    /// </remarks>
    public static Pcm? Decode(byte[] riff) {
        if (riff == null || riff.Length < 12) {
            return null;
        }
        if (Tag(riff, 0) != "RIFF" || Tag(riff, 8) != "WAVE") {
            return null;
        }

        var channels = 0;
        var sampleRate = 0;
        var bits = 0;
        var dataAt = -1;
        var dataLength = 0;

        int at = 12;
        while (at + 8 <= riff.Length) {
            string id = Tag(riff, at);
            int size = BitConverter.ToInt32(riff, at + 4);
            int body = at + 8;
            if (size < 0 || body + size > riff.Length) {
                // A truncated chunk is the end of what we can trust, not a reason to throw.
                break;
            }

            if (id == "fmt " && size >= 16) {
                channels = BitConverter.ToUInt16(riff, body + 2);
                sampleRate = BitConverter.ToInt32(riff, body + 4);
                bits = BitConverter.ToUInt16(riff, body + 14);
            } else if (id == "data") {
                dataAt = body;
                dataLength = size;
            }

            at = body + size + (size & 1);   // chunks are word-aligned
        }

        if (dataAt < 0 || channels <= 0 || sampleRate <= 0) {
            return null;
        }

        return bits switch {
            // *** 8-BIT WAV IS UNSIGNED, 16-BIT IS SIGNED. *** That asymmetry is the format's, and
            // treating the 8-bit case as signed centres it on the wrong value: the waveform comes
            // out inverted around a DC offset, which sounds like loud clipping rather than silence.
            8 => new Pcm(sampleRate, channels, Unsigned8(riff, dataAt, dataLength)),
            16 => new Pcm(sampleRate, channels, Signed16(riff, dataAt, dataLength)),
            _ => null,
        };
    }

    private static float[] Unsigned8(byte[] riff, int at, int length) {
        var samples = new float[length];
        for (var i = 0; i < length; i++) {
            samples[i] = (riff[at + i] - 128) / 128f;
        }
        return samples;
    }

    private static float[] Signed16(byte[] riff, int at, int length) {
        var samples = new float[length / 2];
        for (var i = 0; i < samples.Length; i++) {
            samples[i] = BitConverter.ToInt16(riff, at + (i * 2)) / 32768f;
        }
        return samples;
    }

    private static string Tag(byte[] riff, int at) =>
        System.Text.Encoding.ASCII.GetString(riff, at, 4);
}
