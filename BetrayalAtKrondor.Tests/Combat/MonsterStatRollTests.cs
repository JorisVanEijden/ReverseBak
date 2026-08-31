namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData;
using GameData.Resources.Combat;
using GameData.Resources.Monster;
using Xunit;

/// <summary>
/// Rolling a creature's live stats from MONSTXX.DAT — <c>monstat_roll_stats_from_file</c>.
/// </summary>
/// <remarks>
/// <b>The piece a summon was missing.</b> <c>EnterRoster</c> reads an enemy's stats off its save
/// slot; a conjured creature has neither, so something has to turn the template's RANGES into one
/// creature's numbers.
/// </remarks>
public class MonsterStatRollTests {
    private static MonsterStats Template() => new MonsterStats("MONST01") {
        Health = new StatRange { Min = 10, Max = 20 },
        Stamina = new StatRange { Min = 5, Max = 5 },
        Speed = new StatRange { Min = 1, Max = 2 },
        Strength = new StatRange { Min = 30, Max = 40 },
        AccuracyCrossbow = new StatRange { Min = 41, Max = 41 },
        AccuracyMelee = new StatRange { Min = 42, Max = 42 },
        AccuracyCasting = new StatRange { Min = 43, Max = 43 },
        Defense = new StatRange { Min = 44, Max = 44 },
    };

    [Fact]
    public void TheRangeIsINCLUSIVEAtBothEnds() {
        // RNDR(lo, hi) is lo + rand % (hi - lo + 1). A half-open reading loses the maximum, so on a
        // range like {1,2} the creature could never roll its better value.
        Assert.Equal(1, MonsterStatRoll.RollOne(1, 2, _ => 0));
        Assert.Equal(2, MonsterStatRoll.RollOne(1, 2, n => n - 1));
        Assert.Equal(20, MonsterStatRoll.RollOne(10, 20, n => n - 1));
    }

    [Fact]
    public void APointRangeNeedsNoRoll() {
        var rolls = 0;
        Assert.Equal(7, MonsterStatRoll.RollOne(7, 7, _ => { rolls++; return 0; }));
        Assert.Equal(0, rolls);
    }

    [Fact]
    public void AnINVERTEDRangeIsRefused_notModuloedByANonPositive() {
        // The original would compute `hi - lo + 1` <= 0 here. Shipped data has no inverted range;
        // a mod's might, and the equality guard above it does not catch one.
        Assert.Equal(3, MonsterStatRoll.RollOne(9, 3, n => n - 1));
    }

    [Fact]
    public void THEFILESORDERISNOTTHEATTRIBUTEORDER() {
        // *** The distinguishing case. *** The routine passes stat indices 0,1,2,3 then 5,6,7,4 —
        // so the file's last four ranges land on crossbow, melee, casting and THEN defence.
        // Rolling in file order writes a creature's defence into its crossbow accuracy.
        IReadOnlyDictionary<ActorAttribute, int> rolled =
            MonsterStatRoll.Roll(Template(), _ => 0);

        Assert.Equal(41, rolled[ActorAttribute.AccuracyCrossbow]);
        Assert.Equal(42, rolled[ActorAttribute.AccuracyMelee]);
        Assert.Equal(43, rolled[ActorAttribute.AccuracyCasting]);
        Assert.Equal(44, rolled[ActorAttribute.Defense]);

        // And the naive reading, spelled out: file position 7 is Defense, attribute index 7 is
        // AccuracyCasting. They are not the same slot.
        Assert.Equal(ActorAttribute.Defense, MonsterStatRoll.RolledAttributes[7]);
        Assert.NotEqual((int)ActorAttribute.Defense, 7);
    }

    [Fact]
    public void EveryRolledValueSitsInsideItsOwnRange() {
        foreach (int pick in new[] { 0, 1, 5 }) {
            IReadOnlyDictionary<ActorAttribute, int> rolled =
                MonsterStatRoll.Roll(Template(), n => pick % n);
            Assert.InRange(rolled[ActorAttribute.Health], 10, 20);
            Assert.InRange(rolled[ActorAttribute.Strength], 30, 40);
            Assert.Equal(5, rolled[ActorAttribute.Stamina]);
        }
    }

    [Fact]
    public void CREATURE18WithIntactCategoryTwoGearRollsCreature10SFile() {
        // The first thing the routine does, before it even builds the filename. The creature keeps
        // its own type everywhere else — only its stats come from the other template — so skipping
        // it gives wrong numbers with no other symptom.
        Assert.Equal(10, MonsterStatRoll.TemplateCreatureFor(0x12, true));
        Assert.Equal(0x12, MonsterStatRoll.TemplateCreatureFor(0x12, false));
        Assert.Equal(7, MonsterStatRoll.TemplateCreatureFor(7, true));
    }

    [Fact]
    public void THESUBSTITUTIONIsHowAnArmedCreatureGainsTheSkillToShoot() {
        // *** Not an arbitrary swap. *** The category tested is 2 = Crossbow, and the two templates
        // differ in exactly the stat that gates shooting: read from the shipped files, MONST18's
        // crossbow accuracy is 0 0 — it cannot shoot — while MONST10's is 45 65. Skipping the swap
        // leaves the creature holding a crossbow it can never fire, which reads as a monster that
        // simply never shoots.
        Assert.Equal(ObjectType.Crossbow, MonsterStatRoll.SubstitutionCategory);
        Assert.Equal(2, (int)MonsterStatRoll.SubstitutionCategory);

        // And the gate downstream is Min > 0, which is what a 0 0 range fails.
        var cannotShoot = new StatRange { Min = 0, Max = 0 };
        Assert.False(cannotShoot.Min > 0);
    }

    [Fact]
    public void MORALEIsRolledOnlyWhenItIsAlreadyNonZero_whichIsWhatMakesASummonFearless() {
        // `if (actor->inner->morale != 0)`. MonsterSummon zeroes morale before the roll, so the
        // template's nerve is skipped and the zero survives — the mechanism behind
        // MonsterSummon.Morale, asserted here rather than left implicit two files apart.
        Assert.False(MonsterStatRoll.RollsMorale(MonsterSummon.Morale));
        Assert.True(MonsterStatRoll.RollsMorale(5));
    }

    [Fact]
    public void THEAIPROFILESAreOverwrittenByTheRoll() {
        // Fields 9-11 go straight into the three profile bytes with no guard, three lines after
        // MonsterSummon sets them — which is why that assignment is dead code.
        Assert.False(MonsterStatRoll.AiProfilesSurviveTheRoll);
        Assert.False(MonsterSummon.PatternSurvivesTheStatRoll);
    }

    [Fact]
    public void ANullTemplateRollsNothingRatherThanThrowing() {
        Assert.Empty(MonsterStatRoll.Roll(null, _ => 0));
    }
}
