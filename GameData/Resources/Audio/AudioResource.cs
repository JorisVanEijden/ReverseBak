namespace GameData.Resources.Audio;

public class AudioResource(string id) : IResource {
    public ResourceType Type { get => ResourceType.SND; }
    public string Id { get; } = id;

    public AudioType AudioType { get; set; }
    // public Dictionary<AudioFormat, byte[]> Data { get; } = new();
    public Dictionary<byte, Dictionary<AudioFormat, byte[]>> Variants { get; } = new();
}