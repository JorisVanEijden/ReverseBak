namespace GameData.Resources.Spells;

public class SpellList : IResource, IHaveSubResource<Spell> {
    public SpellList(string id) {
        Id = id;
    }

    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }

    public Dictionary<int, Spell> Spells { get; set; } = [];
    public Spell GetSubResource(int index) {
        return Spells[index];
    }
}