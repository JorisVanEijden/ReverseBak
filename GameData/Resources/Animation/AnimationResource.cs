namespace GameData.Resources.Animation;

public class AnimationResource : IResource {
    public AnimationResource(string id) {
        Id = id;
    }

    public string Version { get; set; }
    public Dictionary<int, string> ResourceFiles { get; set; }
    public Dictionary<int, List<AdsScriptCall>> Script { get; set; }
    public ResourceType Type { get => ResourceType.ADS; }
    public string Id { get; }
    public Dictionary<int, string> Tags { get; set; }
}