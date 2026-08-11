namespace GameData.Resources.Animation;

public record AnimationResource(string Id, string Version, Dictionary<int, string> Tags, List<Frame> Frames) : IResource {
    public string Id { get; } = Id;
    public ResourceType Type { get => ResourceType.TTM; }
    public string Version { get; } = Version;
    public Dictionary<int, string> Tags { get; } = Tags;
    public List<Frame> Frames { get; } = Frames;
}