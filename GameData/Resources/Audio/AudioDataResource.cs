namespace GameData.Resources.Audio;

public record AudioDataResource {
    public byte[]? MidiData { get; set; }
    public byte[]? WavData { get; set; }
    public Dictionary<byte, byte> ChannelFlags { get; set; } = [];
}