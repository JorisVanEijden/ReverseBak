namespace GameData.Resources.Label;

public class LabelSet : IResource {
    public LabelSet(string name) {
        Name = name;
    }

    public string Name { get; set; }

    public List<Label> Labels { get; set; } = new();
    public ResourceType Type { get => ResourceType.LBL; }
}