namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The creature-specific casts that preempt an AI turn — <c>combat_ai_pick_action</c>.
/// </summary>
public class OpportunisticCastsTests {
    private static OpportunisticCasts.Candidate Slayer(int type, bool dead = false,
        bool fleeing = false, bool onGrid = true, bool pathClear = true) =>
        new OpportunisticCasts.Candidate {
            CreatureType = type, IsDead = dead, IsFleeing = fleeing,
            IsOnGrid = onGrid, ProjectilePathIsClear = pathClear,
        };

    [Fact]
    public void ALivingRisenSlayerWithAClearPathTakesTheLivingSpell() {
        Assert.Equal(OpportunisticCasts.SpellAtLivingSlayer,
            OpportunisticCasts.FirstPassSpellFor(
                Slayer(OpportunisticCasts.BlackSlayerRisen), true, true));
    }

    [Fact]
    public void TheTransformingFormIsFOUNDButNeverTakesTheLivingSpell() {
        // The scan matches both forms, and then the live arm excludes 0x17 explicitly. Easy to miss
        // when reading the scan and the arm separately.
        Assert.True(OpportunisticCasts.IsFirstPassType(OpportunisticCasts.BlackSlayerTransforming));
        Assert.Equal(OpportunisticCasts.NoSpell,
            OpportunisticCasts.FirstPassSpellFor(
                Slayer(OpportunisticCasts.BlackSlayerTransforming), true, true));
    }

    [Fact]
    public void ABlockedPathMeansNoLivingSpell() {
        Assert.Equal(OpportunisticCasts.NoSpell,
            OpportunisticCasts.FirstPassSpellFor(
                Slayer(OpportunisticCasts.BlackSlayerRisen, pathClear: false), true, true));
    }

    [Fact]
    public void AFallenSlayerIsRaised_EitherForm() {
        foreach (int type in new[] {
                     OpportunisticCasts.BlackSlayerRisen,
                     OpportunisticCasts.BlackSlayerTransforming }) {
            Assert.Equal(OpportunisticCasts.SpellAtFallenSlayer,
                OpportunisticCasts.FirstPassSpellFor(Slayer(type, dead: true), true, true));
        }
    }

    [Fact]
    public void OneThatFledOrLeftTheGridIsNotRaised() {
        // Same double bar the revival sweep applies: a creature that ran off the field stays off.
        Assert.Equal(OpportunisticCasts.NoSpell, OpportunisticCasts.FirstPassSpellFor(
            Slayer(OpportunisticCasts.BlackSlayerRisen, dead: true, fleeing: true), true, true));
        Assert.Equal(OpportunisticCasts.NoSpell, OpportunisticCasts.FirstPassSpellFor(
            Slayer(OpportunisticCasts.BlackSlayerRisen, dead: true, onGrid: false), true, true));
    }

    [Fact]
    public void AnUncastableSpellFallsThroughToTheOtherArm_NotToNothing() {
        // A living slayer whose spell is uncastable does NOT fall into the raise arm, because that
        // arm requires it to be dead. The two arms are exclusive on the dead flag.
        Assert.Equal(OpportunisticCasts.NoSpell, OpportunisticCasts.FirstPassSpellFor(
            Slayer(OpportunisticCasts.BlackSlayerRisen), livingSpellCastable: false,
            fallenSpellCastable: true));
    }

    [Fact]
    public void TheScanSKIPSOnALowRoll_ItDoesNotActOnOne() {
        // if (RND(100) > 10) break; — a roll of 0..10 walks PAST this creature and keeps looking.
        // Reading it as "act with 90% probability" gets the multi-creature case wrong: a low roll
        // does not mean "do nothing", it means "consider the NEXT one".
        Assert.False(OpportunisticCasts.StopsAtThisMatch(0));
        Assert.False(OpportunisticCasts.StopsAtThisMatch(10));
        Assert.True(OpportunisticCasts.StopsAtThisMatch(11));
        Assert.True(OpportunisticCasts.StopsAtThisMatch(99));
    }

    [Fact]
    public void TheThirdPassBarsAFleeingTargetAndTheSecondDoesNot() {
        // The asymmetry is real and is the kind of thing a tidy-up would "fix".
        var fleeing = new OpportunisticCasts.Candidate {
            CreatureType = OpportunisticCasts.SecondPassType, IsFleeing = true,
        };
        Assert.Equal(OpportunisticCasts.SecondPassSpell,
            OpportunisticCasts.SecondPassSpellFor(fleeing, castable: true));

        var third = new OpportunisticCasts.Candidate {
            CreatureType = 0x29, IsFleeing = true,
        };
        Assert.Equal(OpportunisticCasts.NoSpell,
            OpportunisticCasts.ThirdPassSpellFor(third, castable: true));
    }

    [Fact]
    public void TheThirdPassLooksForThreeTypes() {
        Assert.True(OpportunisticCasts.IsThirdPassType(0x29));
        Assert.True(OpportunisticCasts.IsThirdPassType(0x2a));
        Assert.True(OpportunisticCasts.IsThirdPassType(0x2b));
        Assert.False(OpportunisticCasts.IsThirdPassType(0x2c));
        Assert.False(OpportunisticCasts.IsThirdPassType(OpportunisticCasts.SecondPassType));
    }

    [Fact]
    public void ADeadCandidateTakesNeitherLaterPassesSpell() {
        var dead = new OpportunisticCasts.Candidate { CreatureType = 0x29, IsDead = true };
        Assert.Equal(OpportunisticCasts.NoSpell, OpportunisticCasts.ThirdPassSpellFor(dead, true));
        Assert.Equal(OpportunisticCasts.NoSpell, OpportunisticCasts.SecondPassSpellFor(dead, true));
    }
}
