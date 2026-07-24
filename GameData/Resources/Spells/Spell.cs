namespace GameData.Resources.Spells;

using GameData;

public class Spell : IResource {
    public Spell(string id) {
        Id = id;
    }

    public string Id { get; set; }

    /// <summary>Stable content-graph key of this spell: <c>base:spell:&lt;Id&gt;</c>. The spell
    /// catalog identity that SPELLDOC/spell-symbol references resolve to. See
    /// docs/re-notes/reference-inventory.md #9.</summary>
    public string Key { get; set; } = "";

    /// <summary>De-indexed <see cref="ObjectId"/>: <c>base:objinfo:&lt;ObjectId&gt;</c> when the spell
    /// has an associated inventory object (ObjectId ≥ 0), else null (-1 sentinel = no object). See
    /// docs/re-notes/reference-inventory.md #8.</summary>
    public string? ObjectKey { get; set; }

    public string Name { get; set; }
    public int MinimumCost { get; set; }
    public int MaximumCost { get; set; }
    public bool IsMartial { get; set; }
    public int TargetingType { get; set; }
    public int Color { get; set; }
    public int AnimationEffectType { get; set; }
    public int ObjectId { get; set; }
    public SpellCalculation Calculation { get; set; }
    public int Damage { get; set; }
    public int Duration { get; set; }
    public ResourceType Type { get => ResourceType.DAT; }

    public string ToCsv() {
        return $"{Id},{Name},{MinimumCost},{MaximumCost},{IsMartial},{TargetingType},{Color},{AnimationEffectType},{ObjectId},{Calculation},{Damage},{Duration}";
    }
}