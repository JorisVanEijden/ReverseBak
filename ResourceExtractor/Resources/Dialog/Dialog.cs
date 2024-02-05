namespace ResourceExtractor.Resources.Dialog;

public class Dialog : IResource {
    public string Name { get; set; }
    public Dictionary<int, DialogEntry> Entries { get; set; } = new();
    public ResourceType Type { get => ResourceType.DDX; }
}