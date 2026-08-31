namespace GameData.Resources.Monster;

using System;
using System.Collections.Generic;

/// <summary>
/// Rolling a creature's live stats from its MONSTXX.DAT template —
/// <c>monstat_roll_stats_from_file</c> (MONSTAT.C:24).
/// </summary>
/// <remarks>
/// <b>This is what a summon was missing.</b> <c>CombatRuntime.EnterRoster</c> builds enemies from
/// session roster slots and reads their stats from the save; a conjured creature has no slot and no
/// saved stats, so something has to turn a <see cref="MonsterStats"/> template's RANGES into one
/// creature's numbers. That is this routine, and it is the same one every monster goes through.
/// </remarks>
public static class MonsterStatRoll {
    /// <summary>
    /// One stat: the template's own value when the range is a point, otherwise a roll across it.
    /// </summary>
    /// <param name="rnd">
    /// <c>rnd(n)</c> returning 0..n-1, matching the rest of the port's random seam.
    /// </param>
    /// <remarks>
    /// <c>RNDR(lo, hi)</c> is <c>lo + (rand() &amp; 0xfff) % (hi - lo + 1)</c> — <b>inclusive at
    /// both ends</b>. A half-open reading loses the maximum, which for a one-apart range like {3,4}
    /// means the creature can never roll its best value.
    ///
    /// <para><b>The equality guard is not an optimisation.</b> Without it the modulus would still be
    /// 1 and give the right answer, so it looks redundant — but it is also the only thing standing
    /// between an INVERTED range and a modulo by zero or a negative. Shipped data has none; a mod's
    /// might.</para>
    /// </remarks>
    public static int RollOne(int min, int max, Func<int, int> rnd) {
        if (max == min) {
            return max;
        }
        if (max < min) {
            // The original would compute a non-positive modulus here. Refusing to roll and taking
            // the stated maximum is the least surprising reading of a range someone wrote backwards.
            return max;
        }
        int span = max - min + 1;
        return min + (rnd?.Invoke(span) ?? 0);
    }

    /// <summary>
    /// <b>Creature 0x12 carrying intact category-2 equipment rolls creature 10's file instead.</b>
    /// </summary>
    /// <param name="creatureType">The creature's own type.</param>
    /// <param name="hasIntactCategoryTwoEquipment">
    /// <c>cbstat_find_intact_equip_cat(actor, 2) != NULL</c>.
    /// </param>
    /// <remarks>
    /// The very first thing the routine does, before it builds the filename. It is a genuine
    /// substitution — the creature keeps its own type everywhere else and only its STATS come from
    /// the other template.
    ///
    /// <para><b>And it is not arbitrary: category 2 is <see cref="ObjectType.Crossbow"/>, and the
    /// two templates differ in exactly the stat that matters.</b> Read from the shipped files:
    /// MONST18's crossbow accuracy is <c>0 0</c> — the creature cannot shoot at all — while
    /// MONST10's is <c>45 65</c>. So the swap is how a creature that picks up a crossbow gains the
    /// skill to use it, rather than carrying one it can never fire. (They differ elsewhere too:
    /// MONST18's encounter-AI range is <c>0 0</c> against MONST10's <c>2 7</c>.)</para>
    ///
    /// <para>A port that skips this leaves that creature holding a crossbow with accuracy zero,
    /// which reads as a monster that simply never shoots — no error, no wrong number anyone would
    /// question.</para>
    /// </remarks>
    public static int TemplateCreatureFor(int creatureType, bool hasIntactCategoryTwoEquipment) =>
        creatureType == SubstitutingCreature && hasIntactCategoryTwoEquipment
            ? SubstitutedTemplate
            : creatureType;

    /// <summary>The one creature type whose template can be swapped out.</summary>
    public const int SubstitutingCreature = 0x12;

    /// <summary>The template it reads instead.</summary>
    public const int SubstitutedTemplate = 10;

    /// <summary>
    /// The equipment category the substitution tests — <c>cbstat_find_intact_equip_cat(actor, 2)</c>.
    /// </summary>
    public const ObjectType SubstitutionCategory = ObjectType.Crossbow;

    /// <summary>
    /// The attribute each of the template's first eight ranges is rolled into.
    /// </summary>
    /// <remarks>
    /// <b>The file's order is not the attribute order, and the last four are where they diverge.</b>
    /// The routine reads eight pairs and passes stat indices 0,1,2,3, then <b>5,6,7,4</b> — so the
    /// file's fifth..eighth ranges land on AccuracyCrossbow, AccuracyMelee, AccuracyCasting and
    /// <i>then</i> Defense. Rolling them in file order writes a creature's defence into its crossbow
    /// accuracy and shifts the other three.
    ///
    /// <para><see cref="MonsterStats"/>' properties are declared in FILE order, which is why this
    /// mapping is needed to get from one to the other and why the two look inconsistent side by
    /// side.</para>
    /// </remarks>
    public static readonly IReadOnlyList<ActorAttribute> RolledAttributes = new[] {
        ActorAttribute.Health,
        ActorAttribute.Stamina,
        ActorAttribute.Speed,
        ActorAttribute.Strength,
        ActorAttribute.AccuracyCrossbow,
        ActorAttribute.AccuracyMelee,
        ActorAttribute.AccuracyCasting,
        ActorAttribute.Defense,
    };

    /// <summary>
    /// Rolls the eight stats, in template order, into an attribute-keyed result.
    /// </summary>
    /// <remarks>
    /// <b>Each roll sets BOTH the base and the maximum</b> — <c>stats[i].max = stats[i].base =
    /// result</c>. A creature therefore starts at full health by construction, and its maximum is
    /// its own rolled figure rather than the template's ceiling: two creatures of one kind have
    /// different maxima. Setting only the base leaves the maximum at whatever the stat block was
    /// initialised with, which makes a summon look wounded the moment it lands.
    /// </remarks>
    public static IReadOnlyDictionary<ActorAttribute, int> Roll(MonsterStats template,
        Func<int, int> rnd) {
        var rolled = new Dictionary<ActorAttribute, int>();
        if (template == null) {
            return rolled;
        }

        StatRange[] ranges = {
            template.Health, template.Stamina, template.Speed, template.Strength,
            template.AccuracyCrossbow, template.AccuracyMelee, template.AccuracyCasting,
            template.Defense,
        };
        for (var i = 0; i < ranges.Length && i < RolledAttributes.Count; i++) {
            if (ranges[i] == null) {
                continue;
            }
            rolled[RolledAttributes[i]] = RollOne(ranges[i].Min, ranges[i].Max, rnd);
        }
        return rolled;
    }

    /// <summary>
    /// <b>Morale is rolled ONLY when it is already non-zero.</b>
    /// </summary>
    /// <remarks>
    /// <c>if (actor->inner->morale != '\0')</c>, and it is the last field read. This is the guard
    /// that makes <see cref="Combat.MonsterSummon.Morale"/> stick: the summon routine zeroes morale
    /// before calling the roll, so the template's own nerve is skipped and the creature never routs.
    /// Rolling unconditionally hands every summon a morale and undoes that.
    /// </remarks>
    public static bool RollsMorale(int currentMorale) => currentMorale != 0;

    /// <summary>
    /// The three AI profile fields <b>overwrite</b> whatever the caller set, with no guard.
    /// </summary>
    /// <remarks>
    /// Fields 9, 10 and 11 go straight into <c>aiTurnProfile</c>, <c>aiEncounterProfile</c> and
    /// <c>aiPathProfile</c>. That is why <see cref="Combat.MonsterSummon"/>'s profile assignment is
    /// dead code — it happens three lines before this roll runs over it.
    /// </remarks>
    public static bool AiProfilesSurviveTheRoll => false;
}
