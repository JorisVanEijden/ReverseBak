namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Conjuring a creature onto the combat grid.</summary>
public class MonsterSummonTests {
    [Fact]
    public void AFullRosterMAKESTHESUMMONFAIL_AndTheCastIsStillSpent() {
        // The roster add is tried first and a failure gives up before anything else. Nothing
        // refunds — checking for room before charging would be kinder than the game.
        Assert.False(MonsterSummon.Succeeds(rosterHadRoom: false));
        Assert.True(MonsterSummon.Succeeds(rosterHadRoom: true));
        Assert.Equal(145, MonsterSummon.NoRoomDialog);
    }

    [Fact]
    public void ASUMMONKNOWSNOSPELLS() {
        // All three spell words are zeroed at spawn, so conjuring a caster-kind creature gets its
        // body and not its book. Copying the template's lists makes summons far stronger.
        Assert.False(MonsterSummon.KnowsSpells);
    }

    // g_ai_flee_threshold_table as shipped, same fixture MonsterMoraleTests uses.
    private static readonly int[] ShippedFleeThresholds = { 85, 55, 45, 35, 25, 20, 10, 5, 5, 0 };

    [Fact]
    public void ASummonNeverRouts_BecauseZeroMoraleIsTheOtherNeverFleeValue() {
        // *** THE CORRECTION IS ITSELF CORRECTED. *** A previous pass "withdrew" the natural reading
        // that a summon never routs, on the grounds that the sentinel is 0xff and zero must
        // therefore be the far end of the scale. There are TWO never-flee values: Routs rejects 0xff
        // up front and rejects zero after the roll, and MonsterStats.FleeThreshold documents the
        // same rule from the data side.
        Assert.Equal(0, MonsterSummon.Morale);
        Assert.False(MonsterSummon.Routs);
        Assert.False(MonsterMorale.Routs(staminaPercent: 1, morale: MonsterSummon.Morale,
            rollPercent: 0, ShippedFleeThresholds, isUnderground: false),
            "worst stamina, best possible roll, above ground — still does not run");
    }

    [Fact]
    public void TheZeroMoraleSTICKS_BecauseTheStatRollsMoraleReadIsGuarded() {
        // Why zeroing it is the mechanism rather than a side effect: the MONSTXX.DAT roll reads the
        // creature's own morale only `if (morale != 0)`. Zeroing first is what discards the
        // template's nerve. The AI profiles get no such guard — see below.
        Assert.Equal(0, MonsterSummon.Morale);
        Assert.False(MonsterSummon.PatternSurvivesTheStatRoll);
    }

    [Fact]
    public void ASummonDoesNotActOnTheRoundItLands() {
        // The routine ASSIGNS the flags word rather than OR-ing into it, so Ready is clear. Setting
        // the summon bit on an otherwise ready combatant hands the caster a free extra action.
        Assert.Equal(CombatantFlags.AiSummon, MonsterSummon.InitialFlags);
        Assert.False(MonsterSummon.InitialFlags.HasFlag(CombatantFlags.Ready));
    }

    [Fact]
    public void ASpellCastSummonDoesNotAskWhereToPutIt() {
        // The routine takes a prompt flag and Cast_Spell passes zero: the creature lands on the
        // placement globals. Building a tile-picker into the spell path would add a step the cast
        // does not have.
        Assert.False(MonsterSummon.PromptsForTile(false));
        Assert.True(MonsterSummon.PromptsForTile(true));
    }

    [Fact]
    public void ItPlaysTheSameCreationCueTheLightingSpellsDo() {
        Assert.Equal(58, MonsterSummon.Sound);
    }
}
