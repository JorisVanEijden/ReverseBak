namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.Character;
using GameData.Resources.GameState;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// The party-down byte's readers — <c>g_gameState.bCombatExitRequest</c>, ours
/// <c>PartyDeathState</c>.
/// </summary>
/// <remarks>
/// <b>We had every WRITE and no READ.</b> A pit fall set the flag, the save persisted it, and
/// nothing in the port ever looked at it — so a party wipe changed a byte and the game carried on.
/// These pin what the original does with it.
/// </remarks>
public class PartyDownStateTests {
    [Fact]
    public void ANYNonZeroValueEndsTheLoop_butONLYOneSpeaks() {
        // *** The whole point of the 1/2 split. *** Both loops test != 0 to stop and == 1 to play
        // dialog 0x145. Widening the dialog test to non-zero makes a pit fall show its landing
        // dialog and then this one.
        Assert.False(PartyDownState.EndsTheLoop(PartyDownState.Standing));
        Assert.True(PartyDownState.EndsTheLoop(PartyDownState.Noticed));
        Assert.True(PartyDownState.EndsTheLoop(PartyDownState.Asserted));

        Assert.True(PartyDownState.PlaysTheNoticedDialog(PartyDownState.Noticed));
        Assert.False(PartyDownState.PlaysTheNoticedDialog(PartyDownState.Asserted));
        Assert.False(PartyDownState.PlaysTheNoticedDialog(PartyDownState.Standing));
    }

    [Fact]
    public void THEPITWritesTheSilentValue_whichIsWhyItsOwnDialogIsNotDoubled() {
        Assert.Equal(PartyDownState.Asserted, PitDescent.PartyDeathStateOnFall);
        Assert.False(PartyDownState.PlaysTheNoticedDialog(PitDescent.PartyDeathStateOnFall));
        Assert.NotEqual(PitDescent.LandingDialogId, PartyDownState.NoticedDialogId);
    }

    [Fact]
    public void THEPITStillFiresWhenTheRestOfTheWorldHasGoneQuiet() {
        // *** The asymmetry a single guard would lose. *** WORLDLP.C calls the descent BEFORE the
        // flag test and gates ordinary hotspot activation behind it, so a downed party still falls
        // into a pit it walks onto while nothing else triggers.
        Assert.False(PartyDownState.HotspotsStillFire(PartyDownState.Asserted));
        Assert.True(PartyDownState.HotspotsStillFire(PartyDownState.Standing));
    }

    [Fact]
    public void ADownedPartyRaisesNoAdvancementNotices() {
        // EVTCOND.C returns immediately when the byte is set. A port that keeps sweeping pops
        // "skill improved" dialogs over a party that has just been wiped out.
        Assert.False(PartyDownState.ConditionEventsSweep(PartyDownState.Noticed));
        Assert.False(PartyDownState.ConditionEventsSweep(PartyDownState.Asserted));
        Assert.True(PartyDownState.ConditionEventsSweep(PartyDownState.Standing));
    }

    [Fact]
    public void TheSweepTestsNONZERONearDeath_notFULLNearDeath() {
        // *** The reading that would stop this ever firing from ordinary play. *** STAT.C clears to
        // 0 on the first active member whose rank is zero; it never compares against MaxRank. So a
        // party all slightly near death is down.
        Assert.Equal(PartyDownState.Noticed, PartyDownState.Recompute(new[] { 1, 1, 1 }));
        Assert.Equal(PartyDownState.Noticed,
            PartyDownState.Recompute(new[] { ActorConditions.MaxRank, 1 }));
        Assert.Equal(PartyDownState.Standing, PartyDownState.Recompute(new[] { 1, 0, 1 }));
    }

    [Fact]
    public void ItIsARECOMPUTE_soHealingClearsItAndAlsoErasesTheAssertedMarker() {
        // The sweep assigns rather than latching, so a later run can turn a pit's 2 into a 1 or a 0.
        // The "already spoke" marker does not survive a heal, and nothing tries to make it.
        Assert.Equal(PartyDownState.Standing, PartyDownState.Recompute(new[] { 0 }));
        Assert.NotEqual(PartyDownState.Asserted, PartyDownState.Recompute(new[] { 1, 1 }));
    }

    [Fact]
    public void THEPITSOwnEffectSatisfiesTheSweep() {
        // PitDescent puts every active member at full Near-death, so a sweep straight afterwards
        // would compute Noticed — the same "down", by the other route.
        Assert.Equal(PartyDownState.Noticed, PartyDownState.Recompute(
            new[] { ActorConditions.MaxRank, ActorConditions.MaxRank, ActorConditions.MaxRank }));
    }

    [Fact]
    public void AnEmptyActivePartyReadsAsDown_reproducedRatherThanFixed() {
        // The loop runs partySize times from an initial 1, so zero members leaves the 1. Only
        // reachable in a state the game does not otherwise allow.
        Assert.True(PartyDownState.AnEmptyPartyReadsAsDown);
        Assert.Equal(PartyDownState.Standing, PartyDownState.Recompute(null));
    }
}
