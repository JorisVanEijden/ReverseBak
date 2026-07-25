namespace GameData.Resources.Creature;

using System.Collections.Generic;
using GameData.Resources.Content;

/// <summary>MNAMES.DAT — the creature-name table (the <c>mnames</c> id space, 64 entries). Each entry
/// is a creature id → display name, keyed by its stable content key <c>base:mnames:&lt;Number&gt;</c>.
/// This is the catalog that encounter <c>EnemySlot.CreatureNumber</c> references de-index to (#15).
/// NOTE: this is one of three distinct creature-numbering spaces (see docs/re-notes/reference-inventory.md
/// caveat 1) — it is <b>not</b> interchangeable with SpellAffinity CreatureTypes (0..47) or
/// MonsterStats CreatureId; those need separate reconciliation before any unification.</summary>
public class CreatureNames : IResource {
    public CreatureNames(string id) {
        Id = id;
    }

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }

    public List<CreatureName> Creatures { get; set; } = new();
}

/// <summary>One creature id → name from MNAMES.DAT.</summary>
public class CreatureName {
    /// <summary>Creature id = this entry's index in MNAMES.DAT (0..63).</summary>
    public int Number { get; set; }

    /// <summary>Stable content-graph key: <c>base:mnames:&lt;Number&gt;</c>.</summary>
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";
}
