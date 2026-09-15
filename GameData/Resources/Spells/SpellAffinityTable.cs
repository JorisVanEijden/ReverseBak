namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// The creature × spell affinity tables — the shared format of <b>SPELLWEA.DAT</b> (creature
/// weaknesses) and <b>SPELLRES.DAT</b> (creature resistances), regrouped per spell: for each spell,
/// which creature types are extra-vulnerable to it (weakness) or shrug it off (resistance).
///
/// On-disk layout: <c>u16 rowCount</c> (64), then <c>rowCount × (3 × u16)</c>. <b>Each row is a
/// creature type (the 64 mnames entries) and each of its 48 bits is a spell.</b> The lookups
/// <c>check_spell_weakness</c> (0x6b5db) / <c>check_spell_resistance</c> (0x6b595) compute
/// <c>array[arg1*3 + arg2/16] &amp; (1 &lt;&lt; (arg2%16))</c>, and every caller passes the creature
/// type as <c>arg1</c> — <c>Cast_Evil_Seek</c> @0x673bc pushes the spell number, then
/// <c>combatData.creatureType</c>; canassa's byte-matched CSPELL.C does the same at every site.
/// IDA's parameter NAMES on the two lookups are swapped, and this model once followed them: it read
/// rows as spells, so "spell 58" appeared to affect all 48 creatures. Row 58 is the Nethermander,
/// which resists every spell; rows 15-17 (Gorath, Owyn, Locklear) and the rogues resist Evil Seek.
/// Confirmed live 2026-09-15: the original's Evil Seek on an entry-306 rogue paid its cost and dealt
/// nothing (TASK-541). See <c>docs/FileFormats/SPELLWEA_SPELLRES.DAT.md</c>.
/// </summary>
public class SpellAffinityTable : IResource {
    /// <summary>Creature-type rows in both files (the mnames id space).</summary>
    public const int CreatureTypeCount = 64;

    /// <summary>Spells addressed by one row's 3-word mask (3 × 16).</summary>
    public const int SpellCount = 48;

    public SpellAffinityTable(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per spell (index = spell number).</summary>
    public List<SpellAffinity> Spells { get; set; } = new();

    /// <summary>
    /// Whether this table lists <paramref name="creatureType"/> for <paramref name="spellNumber"/>.
    /// </summary>
    /// <remarks>
    /// The original's <c>check_spell_weakness</c> / <c>check_spell_resistance</c> (ovr177 @0x6b5db,
    /// @0x6b595): <c>mask[spell * 3 + creatureType / 16] &amp; (1 &lt;&lt; (creatureType % 16))</c>.
    /// The extractor has already decoded those bits into <see cref="SpellAffinity.CreatureTypes"/>,
    /// so this is a membership test — but it is the ONE the callers all make, and leaving each
    /// consumer to index <see cref="Spells"/> itself invites an out-of-range spell number becoming
    /// an exception where the original reads a zero bit.
    ///
    /// <para><b>Out of range reads false</b> rather than throwing: the original has no bounds check at
    /// all, so a number outside the table is a data question, not a crash.</para>
    /// </remarks>
    public bool Lists(int spellNumber, int creatureType) {
        if (spellNumber < 0 || spellNumber >= Spells.Count) {
            return false;
        }
        if (creatureType < 0 || creatureType >= CreatureTypeCount) {
            return false;
        }
        return Spells[spellNumber].CreatureTypes.Contains(creatureType);
    }
}

/// <summary>The creature types a single spell is weak/resistant against.</summary>
public class SpellAffinity {
    /// <summary>Spell number: the bit position within a creature row (0..47).</summary>
    public int SpellNumber { get; set; }

    /// <summary>Creature-type rows (0..63) that carry this spell's bit.</summary>
    public List<int> CreatureTypes { get; set; } = new();

    /// <summary>De-indexed <see cref="CreatureTypes"/>: <c>base:mnames:&lt;type&gt;</c> per affected
    /// creature, parallel to <see cref="CreatureTypes"/>. The creature-type index is the mnames
    /// creature number (see docs/re-notes/reference-inventory.md caveat 1). Reference #10.</summary>
    public List<string> CreatureKeys { get; set; } = new();
}
