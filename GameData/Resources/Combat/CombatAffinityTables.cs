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

    /// <summary>
    /// <c>g_aClassCombatGroup</c> (IDA <c>creatures_word_dseg_188E</c> @0x3B65E), indexed by the
    /// combatant's creature class — the ROW selector for <see cref="ClassGroupModifier"/>.
    /// </summary>
    /// <remarks>
    /// <b>Almost every class is group 0.</b> The shipped table is zero everywhere except classes
    /// <b>15, 18 and 21</b>, which are group 2 — and 15 is <b>Gorath</b>. So the one thing this
    /// system visibly does is stop the moredhel taking the mismatch penalty on elven gear, which is
    /// a better story than "a 2% table nobody notices".
    ///
    /// <para><b>Group 1 is unreachable.</b> No creature class selects it, so the middle row of
    /// <see cref="ClassGroupModifier"/> is dead data in the shipped build. Worth knowing before
    /// anyone tries to infer what the three groups "mean" from the modifier table alone.</para>
    /// </remarks>
    public int[] ClassCombatGroup { get; set; } = new int[0];

    /// <summary>
    /// The class-vs-item affinity for a combatant swinging or wearing a given item — the percentage
    /// delta <c>CombatFormulas.MeleeHitChance</c> and <c>ArmorRating</c> take.
    /// </summary>
    /// <param name="creatureClass">The combatant's creature class.</param>
    /// <param name="itemRace">
    /// The item's <c>racialMod</c> field (+0x38), which our <c>ObjectInfo.Race</c> carries verbatim.
    /// </param>
    /// <remarks>
    /// <b>The item's field is a RACE VALUE being used as a COLUMN INDEX, and the two do not have the
    /// same range.</b> <c>ObjectInfo.Race</c> is 0..4 (None, Tsurani, Elf, Human, Dwarf) and the row
    /// is four wide, so:
    /// <list type="bullet">
    ///   <item>Human (3) always lands on the last column, which is <b>-2 for every group</b> — the
    ///     one value that is a penalty regardless of who is holding it. Seventeen shipped items are
    ///     Human-race, including the Broadsword and both non-Tsurani crossbows.</item>
    ///   <item>Dwarf (4) is <b>past the end of the row</b>. The original does not bound-check it:
    ///     <c>racialMods[group * 4 + race]</c> reads on into the next row, or for group 2 past the
    ///     table entirely and into the first word of <see cref="ClassCombatGroup"/>. Two shipped
    ///     items do this — the Sword of Kinnur and one other.</item>
    /// </list>
    ///
    /// <para>Reproduced rather than clamped. Clamping Dwarf to column 3 would turn a -1 into a -2
    /// for most creatures and a 0 into a -2 for the moredhel, which is a real balance change to two
    /// weapons; refusing the lookup would drop the modifier entirely. The out-of-range read is what
    /// the game does, and it is deterministic.</para>
    /// </remarks>
    public int ModifierFor(int creatureClass, int itemRace) {
        if (creatureClass < 0 || creatureClass >= ClassCombatGroup.Length) {
            // The original indexes the class table unchecked too, and everything past its end reads
            // zero for every class that exists — so "group 0" is the faithful answer, not a guard.
            return FlatModifier(0, itemRace);
        }
        return FlatModifier(ClassCombatGroup[creatureClass], itemRace);
    }

    /// <summary>
    /// <c>racialMods[group * ItemGroups + race]</c> over the table read as one flat run, which is
    /// how the original addresses it (<c>bx = group &lt;&lt; 3; bx += race &lt;&lt; 1</c>).
    /// </summary>
    private int FlatModifier(int group, int race) {
        int index = (group * ItemGroups) + race;
        if (index < 0) {
            return 0;
        }

        int row = index / ItemGroups;
        int column = index % ItemGroups;
        if (row < ClassGroupModifier.Length && column < ClassGroupModifier[row].Length) {
            return ClassGroupModifier[row][column];
        }

        // Off the end of the modifier table altogether: the next words in the image are
        // ClassCombatGroup, and the original reads them as if they were modifiers.
        int past = index - (ClassGroupModifier.Length * ItemGroups);
        return past >= 0 && past < ClassCombatGroup.Length ? ClassCombatGroup[past] : 0;
    }

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
