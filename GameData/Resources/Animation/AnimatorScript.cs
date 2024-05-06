namespace GameData.Resources.Animation;

public class AnimatorScript : IResource {
    public AnimatorScript(string id) {
        Id = id;
    }

    public string Version { get; set; }
    public ushort NumberOfFrames { get; set; }
    public Dictionary<int, string> Tags { get; set; }
    public AnimationScript Script { get; set; }
    public ResourceType Type { get => ResourceType.TTM; }
    public string Id { get; }
}