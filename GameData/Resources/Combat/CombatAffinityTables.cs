namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The three combat modifier tables that live in <c>KRONDOR.EXE</c>'s resident data rather than in
/// any <c>.DAT</c> file: how well a class handles a weapon or armour group, and which damage types
/// each creature class is weak or resistant to.
///
/// <para>These are the missing inputs to <c>CombatFormulas</c>: <c>MeleeHitChance</c> and
/// <c>ArmorRating</c> take the class-group modifier, and <c>ApplyDamage</c> takes the weakness and
/// resistance verdicts.</para>
/// </summary>
public class CombatAffinityTables : IResource {
    /// <summary>Creature classes the affinity tables cover.</summary>
    public const int CreatureClassCount = 64;

    /// <summary>Rows in <see cref="ClassGroupModifier"/> — the attacker's class group.</summary>
    public const int ClassGroups = 3;

    /// <summary>Columns in <see cref="ClassGroupModifier"/> — the item's race/group field.</summary>
    public const int ItemGroups = 4;

    public CombatAffinityTables(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>
    /// <c>g_aClassGroupModifier[classGroup][itemGroup]</c> (IDA <c>racialMods?</c> @0x3B646), a
    /// <b>percentage delta</b>: the caller computes <c>value * (modifier + 100) / 100</c>.
    ///
    /// <para>Shipping data is a clean diagonal — 0 where the class group matches the item group,
    /// −1 elsewhere, and −2 for item group 3 throughout. <b>So the effect is at most 2%</b>, which is
    /// worth knowing before building anything on it: this reads like a "racial weapon proficiency"
    /// system and is in practice almost a no-op. Do not scale it up to make it feel meaningful —
    /// that would change combat balance away from the original.</para>
    /// </summary>
    public int[][] ClassGroupModifier { get; set; } = new int[0][];

    /// <summary>Per-creature-class affinities, indexed by class id (0..63).</summary>
    public List<CreatureAffinity> Creatures { get; set; } = new List<CreatureAffinity>();

    /// <summary>
    /// <c>g_anStatCheckThreshold</c> (@0x3B246) — health thresholds the can-cast check sums over.
    ///
    /// <para><b>The shipping values make that check far simpler than it looks.</b> They are
    /// {10, 10, 10, 0, 0, 0, 0, 0, 0}, and the caller passes when health exceeds <i>any</i> of the
    /// nine. Six are zero, so the whole nine-way loop reduces to "health &gt; 0" — the caster is
    /// alive. Worth knowing before anyone builds a difficulty curve on it: there is no curve in the
    /// shipped data, only in the shape of the code.</para>
    /// </summary>
    public int[] StatCheckThresholds { get; set; } = new int[0];

    /// <summary>
    /// <c>g_ai_flee_threshold_table</c> (@0x3B258) — the morale check's flee chance as a percentage,
    /// indexed by the combined stamina-and-morale index.
    ///
    /// <para>{85, 55, 45, 35, 25, 20, 10, 5, 5, 0}: a steep descent, so a creature at the low end
    /// routs on most turns while one at index 9 never does. The index is
    /// <c>staminaPercent/10 - 1 + (8 - morale)</c> clamped to 9 — the morale check recorded on
    /// TASK-97, which had the formula but not this table.</para>
    /// </summary>
    public int[] AiFleeThresholds { get; set; } = new int[0];
}

/// <summary>One creature class's damage-type weaknesses and resistances.</summary>
public class CreatureAffinity {
    /// <summary>Creature class id — the index into both EXE tables.</summary>
    public int ClassId { get; set; }

    /// <summary>
    /// Damage types this class takes <b>half again as much</b> from
    /// (<c>value + value&gt;&gt;1</c>). Matched against the attack's effect mask.
    /// </summary>
    /// <remarks>
    /// From <c>g_aClassProficiencyMask</c> — canassa's name for it, which is misleading: IDA calls it
    /// <c>creatureWeaknessFlags</c> and that is what the body does. The bits are the same effect-mask
    /// space <c>applyDamageToActor</c> uses (1 = poison, 2 = Skin of the Dragon, 4 = Flamecast,
    /// 0x10 = Candle Glow, 0x20 = Grief of 1000 Nights); the rest are not yet identified, so they are
    /// kept raw rather than guessed at.
    /// </remarks>
    public int WeaknessFlags { get; set; }

    /// <summary>Damage types this class takes <b>half</b> from (<c>value &gt;&gt; 1</c>).</summary>
    /// <remarks>From <c>g_aClassWeaknessMask</c> — again inverted in canassa's naming; IDA's
    /// <c>creatureResistanceFlags</c> is correct.</remarks>
    public int ResistanceFlags { get; set; }

    /// <summary>Whether this class has any affinity at all. Most classes have none.</summary>
    public bool IsPlain => WeaknessFlags == 0 && ResistanceFlags == 0;
}
