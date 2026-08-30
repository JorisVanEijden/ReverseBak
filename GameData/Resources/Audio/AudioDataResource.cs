namespace GameData.Resources.Audio;

public record AudioDataResource {
    public byte[]? MidiData { get; set; }
    public byte[]? WavData { get; set; }
    public Dictionary<byte, byte> ChannelFlags { get; set; } = [];

    /// <summary>
    /// Which kind of data this variant holds.
    /// </summary>
    /// <remarks>
    /// <b>Derived, because the file has no format field</b> — the parser decides by the channel a
    /// track arrives on (<see cref="AudioChannel.DigitisedSample"/>) and then stores the result in
    /// one of the two payloads. Reading it back off the payload keeps the answer in one place
    /// instead of every caller re-testing for null.
    ///
    /// <para><b>WAV wins when both are set</b>, which a variant can be: the same variant may carry
    /// a digitised sample AND midi tracks, because the sentinel channel and the real ones are read
    /// in the same loop. The sample is the one that plays.</para>
    /// </remarks>
    public AudioFormat? Format =>
        WavData != null ? AudioFormat.Wav
        : MidiData != null ? AudioFormat.Midi
        : null;
}
