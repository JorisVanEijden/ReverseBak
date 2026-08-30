namespace BetrayalAtKrondor.Tests.Combat;

using System;
using System.Collections.Generic;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The three creature-specific passes an AI caster runs before anything else on its turn —
/// <c>combat_ai_pick_action</c>.
/// </summary>
public class OpportunisticCastsTests {
    private static OpportunisticCasts.Candidate Enemy(int type, bool dead = false,
        bool fleeing = false, bool onGrid = true, bool path = true) =>
        new() {
            CreatureType = type,
            IsDead = dead,
            IsFleeing = fleeing,
            IsOnGrid = onGrid,
            ProjectilePathIsClear = path,
        };

    /// <summary>Never skips a match and can cast anything.</summary>
    private static OpportunisticCasts.Cast Run(params OpportunisticCasts.Candidate[] enemies) =>
        OpportunisticCasts.Choose(enemies, _ => 100, _ => true);

    [Fact]
    public void ALivingBlackSlayerDrawsTheSpellNamedAfterIt() {
        OpportunisticCasts.Cast cast = Run(Enemy(OpportunisticCasts.BlackSlayerRisen));
        Assert.Equal(OpportunisticCasts.SpellAtLivingSlayer, cast.SpellId);
        Assert.Equal(0, cast.TargetIndex);
    }

    [Fact]
    public void AFallenBlackSlayerStillOnTheGridDrawsFinalRest() {
        // The counter to SlayerRevival: lay it to rest before the countdown gets it back up.
        OpportunisticCasts.Cast cast = Run(Enemy(OpportunisticCasts.BlackSlayerRisen, dead: true));
        Assert.Equal(OpportunisticCasts.SpellAtFallenSlayer, cast.SpellId);
    }

    [Fact]
    public void ACorpseThatFledOrLeftTheGridIsNotWorthTheSpell() {
        // Exactly the pair of bars SlayerRevival applies, because those are the corpses that rise.
        Assert.False(Run(Enemy(OpportunisticCasts.BlackSlayerRisen, dead: true, fleeing: true)).Fires);
        Assert.False(Run(Enemy(OpportunisticCasts.BlackSlayerRisen, dead: true, onGrid: false)).Fires);
    }

    [Fact]
    public void TheTransformingFormIsSpAredWhileAliveButNotWhileDead() {
        // The living arm excludes 0x17 explicitly; the fallen arm takes either form.
        Assert.False(Run(Enemy(OpportunisticCasts.BlackSlayerTransforming)).Fires);
        Assert.Equal(OpportunisticCasts.SpellAtFallenSlayer,
            Run(Enemy(OpportunisticCasts.BlackSlayerTransforming, dead: true)).SpellId);
    }

    [Fact]
    public void ALivingSlayerBehindCoverIsNotShotAt() {
        Assert.False(Run(Enemy(OpportunisticCasts.BlackSlayerRisen, path: false)).Fires);
    }

    [Fact]
    public void AMatchedPassThatCannotCastFallsThroughToTheNextOne() {
        // *** The regression this guards. *** Returning "nothing" as soon as a pass MATCHED would
        // silence passes two and three whenever a Black Slayer happened to be on the field — the
        // original's first arm ends in `goto L_phase2`, not a return.
        var field = new List<OpportunisticCasts.Candidate> {
            Enemy(OpportunisticCasts.BlackSlayerRisen),
            Enemy(OpportunisticCasts.SecondPassType),
        };
        OpportunisticCasts.Cast cast = OpportunisticCasts.Choose(field, _ => 100,
            spell => spell == OpportunisticCasts.SecondPassSpell);
        Assert.Equal(OpportunisticCasts.SecondPassSpell, cast.SpellId);
        Assert.Equal(1, cast.TargetIndex);
    }

    [Fact]
    public void TheSkipRollWalksPastAMatchAboutOneTimeInTen() {
        // `if (RND(100) > 10) break;` — a roll of 0..10 keeps looking. It is a chance to SKIP,
        // not a chance to act, which is why one lone creature means the pass sometimes does
        // nothing at all rather than acting 90% of the time.
        Assert.False(OpportunisticCasts.StopsAtThisMatch(10));
        Assert.True(OpportunisticCasts.StopsAtThisMatch(11));
        Assert.False(OpportunisticCasts.Choose(
            new[] { Enemy(OpportunisticCasts.BlackSlayerRisen) }, _ => 0, _ => true).Fires);
    }

    [Fact]
    public void ASkippedFirstMatchLetsTheScanSettleOnTheSecond() {
        // Two matches, and the roll walks past the first: the pass acts on the LATER one rather
        // than on the nearest, which is what "act with 90% probability" gets wrong.
        var rolls = new Queue<int>(new[] { 0, 100 });
        var field = new[] {
            Enemy(OpportunisticCasts.BlackSlayerRisen),
            Enemy(OpportunisticCasts.BlackSlayerRisen),
        };
        OpportunisticCasts.Cast cast = OpportunisticCasts.Choose(field, _ => rolls.Dequeue(), _ => true);
        Assert.Equal(1, cast.TargetIndex);
    }

    [Fact]
    public void TheThirdPassBarsAFleeingTargetAndTheSecondDoesNot() {
        // The one asymmetry between the two later passes, transcribed rather than tidied.
        Assert.Equal(OpportunisticCasts.SecondPassSpell,
            Run(Enemy(OpportunisticCasts.SecondPassType, fleeing: true)).SpellId);
        Assert.False(Run(Enemy(OpportunisticCasts.ThirdPassTypes[0], fleeing: true)).Fires);
        Assert.Equal(OpportunisticCasts.ThirdPassSpell,
            Run(Enemy(OpportunisticCasts.ThirdPassTypes[0])).SpellId);
    }

    [Fact]
    public void AnOrdinaryPartyMemberDrawsNothingAtAll() {
        // Every pass hunts a specific monster creature type, so a monster caster looking at the
        // party finds nobody. That is correct, and it is why this routine is mostly the PARTY's
        // tactic — reached when auto-resolve plays the party through the same AI.
        Assert.False(Run(Enemy(0), Enemy(1), Enemy(2)).Fires);
    }

    [Fact]
    public void NoCandidatesIsNotACrash() {
        Assert.False(OpportunisticCasts.Choose(Array.Empty<OpportunisticCasts.Candidate>(),
            _ => 100, _ => true).Fires);
        Assert.False(OpportunisticCasts.Choose(null, _ => 100, _ => true).Fires);
    }
}
