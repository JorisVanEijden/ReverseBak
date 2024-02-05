namespace ResourceExtractor.Resources.Animation;

public class AnimationResource : IResource {
    public string Version { get; set; }
    public Dictionary<int, string> ResourceFiles { get; set; }
    public List<string> Script { get; set; }
    public ResourceType Type { get => ResourceType.ADS; }
}