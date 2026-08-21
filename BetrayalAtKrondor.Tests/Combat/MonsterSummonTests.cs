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

    [Fact]
    public void TheFleeThresholdIsZEROED_AndZeroIsNotTheNeverFleesSentinel() {
        // *** The correction. *** Zeroing looks like "fearless" and is not: the never-flees value is
        // 0xff, so zero is the OTHER end of the scale. What is certain is only that the template's
        // value is discarded — what zero then means belongs to MonsterMorale.
        Assert.Equal(0, MonsterSummon.FleeThreshold);
        Assert.NotEqual(MonsterMorale.NeverFleesMorale, MonsterSummon.FleeThreshold);
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
