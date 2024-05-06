namespace GameData.Resources.Dialog;

public class Dialog : IResource {
    public Dialog(string id) {
        Id = id;
    }

    public Dictionary<int, DialogEntry> Entries { get; set; } = new();
    public ResourceType Type { get => ResourceType.DDX; }
    public string Id { get; }
}