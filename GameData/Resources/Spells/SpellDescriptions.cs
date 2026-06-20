namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// SPELLDOC.DAT — the spell-description text shown in the spell-info panel
/// (<c>UI_show_spell_info</c>, ovr173 @ 0x6950f). Reversed from the second half of
/// <c>Load_spells</c> (ovr173 @ 0x66700).
///
/// On disk: u16 entryCount, u32 offset[entryCount] (blob-relative), u16 declaredSize
/// (= whole file size — an over-alloc quirk, the blob actually runs to EOF), then a
/// NUL-terminated string blob. The entries are a flat offset table, but the UI indexes them
/// as <b>7 fixed lines per spell</b> (<c>base = spellNumber * 7</c>), so this model groups
/// them into one <see cref="SpellDescription"/> per spell (entryCount / 7 spells; unused
/// spell slots are all-blank and kept so the index lines up with the spell number).
/// See <c>docs/FileFormats/SPELLDOC.DAT.md</c>.
/// </summary>
public class SpellDescriptions : IResource {
    /// <summary>Lines per spell in the on-disk offset table (the UI's <c>*7</c> stride).</summary>
    public const int FieldsPerSpell = 7;

    public SpellDescriptions(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per spell, indexed by spell number.</summary>
    public List<SpellDescription> Spells { get; set; } = new();
}

/// <summary>
/// The 7 description lines for one spell. <see cref="Name"/> is the centred panel title;
/// the rest are body lines drawn top-to-bottom (blank lines are skipped). The
/// <see cref="Cost"/> and <see cref="Damage"/> lines are static template text that the
/// game <b>overrides at runtime</b> with the actual computed values
/// (<c>"Cost: %d Health+Stamina"</c> / <c>"Damage: %d"</c>); the remaining lines are shown
/// verbatim.
/// </summary>
public class SpellDescription {
    /// <summary>Spell number (this entry's index).</summary>
    public int SpellNumber { get; set; }

    /// <summary>Field 0 — spell name (centred panel title).</summary>
    public string Name { get; set; } = "";

    /// <summary>Field 1 — cost template (e.g. "Cost: 5-15 Health/Stamina"); replaced at
    /// runtime with the actual cost when one is supplied.</summary>
    public string Cost { get; set; } = "";

    /// <summary>Field 2 — damage template (e.g. "Damage: 3 x Cost" / "Damage: None");
    /// replaced at runtime with "Damage: N" from the computed magnitude (unless 0/1000).</summary>
    public string Damage { get; set; } = "";

    /// <summary>Field 3 — duration line (e.g. "Duration: 10 minutes X Cost"); shown verbatim.</summary>
    public string Duration { get; set; } = "";

    /// <summary>Field 4 — line-of-sight requirement (e.g. "Line of sight: Yes"); shown verbatim.</summary>
    public string LineOfSight { get; set; } = "";

    /// <summary>Field 5 — effect description, first line (e.g. "Area fire damage").</summary>
    public string Effect { get; set; } = "";

    /// <summary>Field 6 — effect description, second line / extra note (e.g. "Could kill
    /// caster."); empty when the effect fits one line.</summary>
    public string EffectLine2 { get; set; } = "";
}
