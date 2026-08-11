namespace GameData.Resources.Dialog;

public class Dialog(string id) : IResource {
    public ResourceType Type { get => ResourceType.DDX; }
    public string Id { get; } = id;
    public List<DialogEntry> Entries { get; set; } = [];
}