namespace GameData.Resources.Animation;

public record AnimatorResource : IResource {
    public AnimatorResource(string id) {
        Id = id;
    }

    public string Version { get; set; }
    public Dictionary<int, string> ResourceFiles { get; set; }
    public List<AnimatorScript> Animations { get; set; } = [];
    public ResourceType Type { get => ResourceType.ADS; }
    public string Id { get; }
}