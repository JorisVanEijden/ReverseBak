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
    /// <summary>
    /// The spell's kind (<c>nSpell_kind</c>), which decides how <see cref="EffectSubject"/> is
    /// read — see there.
    /// </summary>
    public int TargetingType { get; set; }

    /// <summary>
    /// The polymorphic +10 field (<c>nEffect_sprite_id</c>). <b>Its meaning depends on
    /// <see cref="TargetingType"/></b>, so it must never be rendered as one thing:
    /// <list type="bullet">
    /// <item>kind 5 — a combat-grid tile effect id (<c>combatgrid_place_tile_fx_at_cur</c>);
    /// see <see cref="TileEffectId"/>.</item>
    /// <item>kind 6 — the <b>creature</b> to summon (<c>cspell_summon_monster</c>); see
    /// <see cref="SummonedCreatureId"/>.</item>
    /// <item>the visual kinds — a sprite / VGA colour index, used by the palette flash, fade and
    /// particle-blast paths (CSPELL.C:795-840).</item>
    /// </list>
    /// <para>Was called <c>Color</c>, which is right only for that last group: a consumer reading
    /// Arachnos's 44 as a colour would render nonsense, when it means <c>spider</c>.</para>
    /// </summary>
    public int EffectSubject { get; set; }

    /// <summary>
    /// The creature this spell summons, or null when it is not a summoning spell. Resolves against
    /// the monster-name catalog (<c>base:mnames:&lt;id&gt;</c>): the four shipped summons are
    /// Arachnos 44 (spider), River Song 56 (Rusalki), and Grave's Disquiet / Summon Riftmare 57
    /// (Shade).
    /// </summary>
    public int? SummonedCreatureId =>
        TargetingType == SummonKind && EffectSubject >= 0 ? EffectSubject : null;

    /// <summary>
    /// The combat-grid tile effect this spell places, or null when it places none. Mirrorwall and
    /// Gambit of the Eight are the shipped users; Asphyxiation is kind 5 with no tile effect.
    /// </summary>
    public int? TileEffectId =>
        TargetingType == TileEffectKind && EffectSubject >= 0 ? EffectSubject : null;

    private const int TileEffectKind = 5;
    private const int SummonKind = 6;
    public int AnimationEffectType { get; set; }
    public int ObjectId { get; set; }
    public SpellCalculation Calculation { get; set; }
    public int Damage { get; set; }
    public int Duration { get; set; }
    public ResourceType Type { get => ResourceType.DAT; }

    public string ToCsv() {
        return $"{Id},{Name},{MinimumCost},{MaximumCost},{IsMartial},{TargetingType},{EffectSubject},{AnimationEffectType},{ObjectId},{Calculation},{Damage},{Duration}";
    }
}