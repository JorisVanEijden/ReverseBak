namespace ResourceExtraction.Extractors.Audio;

public struct MidiEvent() {
    public int DeltaTime { get; set; }
    public byte Status { get; set; }
    public byte[] Data { get; set; } = [];
}