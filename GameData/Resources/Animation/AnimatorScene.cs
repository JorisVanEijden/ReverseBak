namespace GameData.Resources.Animation;

using GameData.Resources.Animation.Commands;

public class AnimatorScene : IResource {
    public AnimatorScene(string id) {
        Id = id;
    }

    public string Version { get; set; }
    public ushort NumberOfFrames { get; set; }
    public Dictionary<int, string> Tags { get; set; }
    public List<List<FrameCommand>> Frames { get; set; } = [];
    public ResourceType Type { get => ResourceType.TTM; }
    public string Id { get; }
}