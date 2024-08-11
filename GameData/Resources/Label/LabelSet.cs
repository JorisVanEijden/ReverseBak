namespace GameData.Resources.Label;

public class LabelSet : IResource {
    public LabelSet(string id) {
        Id = id;
    }

    public List<Label> Labels { get; set; } = [];
    public ResourceType Type { get => ResourceType.LBL; }
    public string Id { get; }
}