namespace GameData.Resources.Audio;

public class AudioResource(string id) : IResource {
    public ResourceType Type { get => ResourceType.SND; }
    public string Id { get; } = id;
    public AudioType AudioType { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<byte, AudioDataResource> Variants { get; } = new();
}