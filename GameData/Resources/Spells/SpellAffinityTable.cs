namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// A per-spell creature-type affinity bitmask table — the shared format of
/// <b>SPELLWEA.DAT</b> (creature weaknesses) and <b>SPELLRES.DAT</b> (creature
/// resistances). For each spell it records which creature types are extra-vulnerable to
/// (weakness) or shrug off (resistance) that spell.
///
/// On-disk layout: <c>u16 spellCount</c> (64), then <c>spellCount × (3 × u16)</c> — a
/// 48-bit creature-type bitmask per spell.
///
/// Reversed from <c>Load_spell_weakness_and_resistance</c> (ovr177 @ 0x6b4dc) and the
/// lookups <c>check_spell_weakness</c> (0x6b5db) / <c>check_spell_resistance</c> (0x6b595):
/// the test is <c>array[spell*3 + creatureType/16] &amp; (1 &lt;&lt; (creatureType%16))</c>,
/// i.e. bit <c>N</c> across the three words = creature type <c>N</c> (0..47). The extractor
/// decodes the masks into the explicit list of affected creature-type indices. See
/// <c>docs/FileFormats/SPELLWEA_SPELLRES.DAT.md</c>.
/// </summary>
public class SpellAffinityTable : IResource {
    /// <summary>Creature types addressed by the 3-word mask (3 × 16).</summary>
    public const int CreatureTypeCount = 48;

    public SpellAffinityTable(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per spell (index = spell number).</summary>
    public List<SpellAffinity> Spells { get; set; } = new();
}

/// <summary>The creature types a single spell is weak/resistant against.</summary>
public class SpellAffinity {
    /// <summary>Spell number (this entry's index in the file).</summary>
    public int SpellNumber { get; set; }

    /// <summary>Creature-type indices (0..47) whose bit is set in this spell's mask.</summary>
    public List<int> CreatureTypes { get; set; } = new();
}
