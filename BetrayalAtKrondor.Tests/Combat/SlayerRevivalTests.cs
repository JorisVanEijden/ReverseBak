namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// The fallen getting back up. The species change and the presence gate are the two rules that make
/// this encounter behave the way it does.
/// </summary>
public class SlayerRevivalTests {
    private const int Dead = SlayerRevival.DeadFlag;

    [Fact]
    public void AFallenNighthawkRisesAsABlackSlayer() {
        // Not a restore — a species change. What gets up is not what went down.
        Assert.Equal((int)CreatureType.BlackSlayer,
            SlayerRevival.TypeAfterRising((int)CreatureType.Nighthawk));
    }

    [Fact]
    public void ABlackSlayerRisesAsItself() {
        Assert.Equal((int)CreatureType.BlackSlayer,
            SlayerRevival.TypeAfterRising((int)CreatureType.BlackSlayer));
    }

    [Fact]
    public void TheChangeIsOneWay() {
        int once = SlayerRevival.TypeAfterRising((int)CreatureType.Nighthawk);

        Assert.Equal(once, SlayerRevival.TypeAfterRising(once));
    }

    [Fact]
    public void NothingRisesUnlessOneIsAlreadyInTheFight() {
        // An encounter with only Nighthawks in it never sees a single revival.
        Assert.False(SlayerRevival.SweepRuns(0));
        Assert.True(SlayerRevival.SweepRuns(1));
    }

    [Fact]
    public void OnlyTheTwoSpeciesAreEligible() {
        Assert.True(SlayerRevival.IsEligibleSpecies((int)CreatureType.BlackSlayer));
        Assert.True(SlayerRevival.IsEligibleSpecies((int)CreatureType.Nighthawk));
        Assert.False(SlayerRevival.IsEligibleSpecies((int)CreatureType.Rogue));
        Assert.False(SlayerRevival.IsEligibleSpecies((int)CreatureType.MoredhelWarrior));
    }

    [Fact]
    public void OnlyADeadActorIsACandidate() {
        Assert.True(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk, Dead));
        Assert.False(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk, 0));
    }

    [Fact]
    public void OneThatFledIsBarredEvenThoughItIsDead() {
        // Barred twice over: it keeps the flag, and the exit path kills it without the death
        // animation, which is the only place a countdown is ever set.
        Assert.False(SlayerRevival.IsCandidate((int)CreatureType.Nighthawk,
            Dead | SlayerRevival.FledFlag));
    }

    [Fact]
    public void KillingTheRisersDoesNotEndTheMechanic() {
        // The sweep counts by creature type with no alive test, and the combatant list never
        // shrinks — so a dead Black Slayer keeps it running AND is itself an eligible corpse.
        Assert.True(SlayerRevival.SweepRuns(1));
        Assert.True(SlayerRevival.IsCandidate((int)CreatureType.BlackSlayer, Dead));
    }

    [Fact]
    public void TheCountdownRolledAtDeathIsFourToTen() {
        Assert.Equal(4, SlayerRevival.MinimumCountdown);
        Assert.Equal(10, SlayerRevival.MaximumCountdown);
        Assert.True(SlayerRevival.MinimumCountdown > 0,
            "a zero roll would let a corpse rise on the very tick it fell");
    }

    [Fact]
    public void TheCountdownMustReachZeroFirst() {
        Assert.False(SlayerRevival.RisesThisTick(1, 4));
        Assert.True(SlayerRevival.RisesThisTick(0, 4));
    }

    [Fact]
    public void AnActorOffTheGridCountsDownForeverAndNeverRises() {
        // The position test sits alongside the countdown rather than before it, so leaving the field
        // does not cancel the wait — it just never ends.
        Assert.False(SlayerRevival.RisesThisTick(0, SlayerRevival.OffGrid));
    }

    [Fact]
    public void ABlockedTileKeepsTheBodyDownWithoutCancellingIt() {
        // The countdown has already reached zero, so it is retried every tick until the tile clears.
        Assert.False(SlayerRevival.CanRiseOnTile(tileBlocked: true));
        Assert.True(SlayerRevival.CanRiseOnTile(tileBlocked: false));
    }

    [Fact]
    public void WhatGetsUpIsAtFullStrength() {
        Assert.True(SlayerRevival.RisesAtFullStrength);
    }

    [Fact]
    public void RisingLeavesAHazardOnTheTile() {
        Assert.Equal(9, SlayerRevival.RisenTileEffect);
        Assert.Equal(400, SlayerRevival.RisenTileEffectDuration);
    }

    [Fact]
    public void RisingASSIGNSReadyRatherThanORingIt() {
        // *** THE ONE LINE A PORT GETS WRONG. *** flags = CAF_READY (CBTAIACT.C:303) is a plain
        // assignment, so Dead, Fleeing, Poison, Parry and Knockback all go together. `flags |=
        // Ready` — what "it gets up ready to act" suggests, and what the same subsystem does in four
        // OTHER places — leaves the Dead bit set: a fully-healed corpse that never acts, never
        // counts as a live enemy and can never be killed again.
        CombatantFlags before = CombatantFlags.Dead | CombatantFlags.Fleeing
            | CombatantFlags.Poisoned | CombatantFlags.Parry;

        CombatantFlags after = SlayerRevival.FlagsAfterRising(before);

        Assert.Equal(CombatantFlags.Ready, after);
        Assert.False(after.HasFlag(CombatantFlags.Dead), "the corpse flag above all");
        Assert.False(after.HasFlag(CombatantFlags.Poisoned), "and the poison it died of");
    }

    [Fact]
    public void TheRiseClearsItsOwnCountdown() {
        // dmgFloatFrames IS the revival countdown, and the rise zeroes it — so a risen creature
        // that falls again starts a fresh count rather than rising instantly.
        Assert.True(SlayerRevival.ClearsItsOwnCountdown);
    }

    [Fact]
    public void TheRiseSoundIsSEVENTYONE_TheIdTheRoutinePushes() {
        // combataiact_bhood_revive_cycle plays audio_play(0x47) between removing the actor from the
        // grid and running its VFX (CBTAIACT.C:287). Pinned as the decimal the archive keys on as
        // well, because the resource name is the DECIMAL string while the disassembly and this
        // constant are hex — 0x47 loads as "71".
        Assert.Equal(71, SlayerRevival.RisingSound);
    }

    [Fact]
    public void ABodyUnderABLOCKEDTileRisesSilently_BecauseItDoesNotRise() {
        // *** THE CUE SITS INSIDE THE TILE TEST. *** The whole cycle is wrapped in
        // `if (combatgrid_tile_is_blocked(...) == 0)`, and the countdown has already run out by the
        // time CanRiseOnTile is consulted — so the attempt is retried EVERY tick while somebody
        // stands on the grave. A cue on the attempt rather than the rise would tick once a round
        // for as long as that lasted.
        Assert.False(SlayerRevival.CanRiseOnTile(tileBlocked: true));
        Assert.True(SlayerRevival.CanRiseOnTile(tileBlocked: false));
    }

    [Fact]
    public void TheRisenSpeciesSoundsTheSameAsTheMorphingOne() {
        // The cue plays before the species change and unconditionally of it, so a creature that was
        // already the risen type sounds exactly like one transforming into it. Gating the sound on
        // TypeAfterRising changing something would silence every riser of the first kind.
        Assert.Equal(SlayerRevival.RisenType, SlayerRevival.TypeAfterRising(SlayerRevival.RisenType));
        Assert.Equal(SlayerRevival.RisenType,
            SlayerRevival.TypeAfterRising(SlayerRevival.TransformingType));
        Assert.True(SlayerRevival.IsEligibleSpecies(SlayerRevival.RisenType));
        Assert.True(SlayerRevival.IsEligibleSpecies(SlayerRevival.TransformingType));
    }
}
