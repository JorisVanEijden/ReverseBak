namespace GameData.Resources.Creature;

using System.Collections.Generic;
using GameData.Resources.Content;

/// <summary>MNAMES.DAT — the creature-name table (the <c>mnames</c> id space, 64 entries). Each entry
/// is a creature id → display name, keyed by its stable content key <c>base:mnames:&lt;Number&gt;</c>.
/// This is the catalog that encounter <c>EnemySlot.CreatureNumber</c> references de-index to (#15).
/// RESOLVED 2026-07-25 (IDA): the mnames number is the <b>single</b> canonical creature id, held at
/// runtime as <c>combatData.creatureType</c>. It also names the MonsterStats file
/// (<c>MONST{n}.DAT</c>) and indexes the SPELLWEA/SPELLRES affinity mask — i.e. MonsterStats.CreatureId
/// and SpellAffinity CreatureTypes are the <b>same</b> space, not separate (the affinity "0..47" is just
/// the 48-bit mask width). So SpellAffinity.CreatureTypes[] (#10) also keys to <c>base:mnames:&lt;n&gt;</c>.
/// See docs/re-notes/reference-inventory.md caveat 1 and the IDA anchor @0x6a3b2.</summary>
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
