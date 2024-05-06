namespace GameData.Resources.Spells;

public class SpellInfoList : IResource {
    public SpellInfoList(string id) {
        Id = id;
    }

    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }

    public Dictionary<int, SpellInfo> List { get; set; } = [];
}