namespace GameData.Resources.Audio;

/// <summary>Which kind of sound data a variant holds.</summary>
/// <remarks>
/// <b>The file does not store this; it is DERIVED from the channel a track is on.</b> A track
/// whose channel byte is <see cref="AudioChannel.DigitisedSample"/> carries a WAV, and every other
/// channel carries MIDI events — so the distinction is a sentinel channel number rather than a
/// format field, and a reader looking for one will not find it.
/// </remarks>
public enum AudioFormat {
    Midi,
    Wav,
}

/// <summary>Channel numbers with a meaning of their own.</summary>
public static class AudioChannel {
    /// <summary>
    /// The channel a digitised sample arrives on, rather than a real MIDI channel.
    /// </summary>
    /// <remarks>
    /// <b>0xFE is the whole discriminator between a WAV and a MIDI track</b>, and it was a bare
    /// magic number in the extractor. MIDI channels are 0..15, so this is far outside the range
    /// that could be one — it is a marker, not a channel the synth ever addresses.
    /// </remarks>
    public const byte DigitisedSample = 0xFE;

    /// <summary>Ends a list of tracks, or of variants.</summary>
    /// <remarks>Both loops in the parser terminate on this, one nested inside the other.</remarks>
    public const byte EndOfList = 0xFF;
}
