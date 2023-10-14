namespace ResourceExtractor.Resources.Animation;

public class AnimationResource : IResource {
    public string Version { get; set; }
    public byte[] ScriptBytes { get; set; }
    public ResourceType Type { get => ResourceType.ADS; }
    public Dictionary<int, string> ResourceFiles { get; set; }
}