namespace GameData.Resources.Palette;

public class RemapResource : IResource {
    public RemapResource(string id) {
        Id = id;
        Mappings = new Dictionary<int, Dictionary<byte, byte>>();
    }

    public Dictionary<int, Dictionary<byte, byte>> Mappings { get; set; }

    public ResourceType Type {
        get => ResourceType.RMP;
    }

    public string Id { get; }
}