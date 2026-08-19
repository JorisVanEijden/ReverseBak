namespace BetrayalAtKrondor.Tests.World;

using GameData;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Walking past a fight — <c>combTrigger_phase2</c>'s avoidance block.
/// </summary>
public class CombatEncounterAvoidanceTests {
    [Fact]
    public void AnAvoidableEncounterSTILLHasToHaveBeenSpotted() {
        // The flag is permission to try, not a free pass. Reading it as "avoidable means you can
        // sneak past" makes 62 shipped encounters skippable that are not.
        Assert.True(CombatEncounterAvoidance.MayAttempt(
            avoidable: true, scouted: true, dragonsBreathActive: false));
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: true, scouted: false, dragonsBreathActive: false));
    }

    [Fact]
    public void TheTwoRoutesIntoTheRollAreDISJOINT() {
        // Scouting works only on flagged encounters, the fog only on unflagged ones. Neither is a
        // general "avoid" mechanic, and a rule that ORs them together lets a scouted party sneak
        // past everything.
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: false, scouted: true, dragonsBreathActive: false));
        Assert.True(CombatEncounterAvoidance.MayAttempt(
            avoidable: false, scouted: false, dragonsBreathActive: true));
    }

    [Fact]
    public void TheStatBonusIsGATEDOnTheRawValueNotTheResult() {
        // At or above ninety there is no bonus at all — the test is on the stat itself.
        Assert.Equal(90, CombatEncounterAvoidance.Chance(90, avoidable: false, dragonsBreathActive: false));
        Assert.Equal(95, CombatEncounterAvoidance.Chance(95, avoidable: false, dragonsBreathActive: false));
    }

    [Fact]
    public void TheBonusIsThirtyPercentAndClampsAtNinety() {
        Assert.Equal(52, CombatEncounterAvoidance.Chance(40, avoidable: false, dragonsBreathActive: false));
        Assert.Equal(90, CombatEncounterAvoidance.Chance(80, avoidable: false, dragonsBreathActive: false));
    }

    [Fact]
    public void DRAGONSBREATHAddsItsBonusOnlyToAnAVOIDABLEEncounter() {
        // The mirror of the gate: on an unflagged encounter the fog is what lets the party roll and
        // contributes nothing to the roll it unlocked. Applying it in both cases hands the fog a
        // bonus exactly where the original gives none.
        int unflagged = CombatEncounterAvoidance.Chance(40, avoidable: false, dragonsBreathActive: true);
        int flagged = CombatEncounterAvoidance.Chance(40, avoidable: true, dragonsBreathActive: true);

        Assert.Equal(52, unflagged);
        Assert.Equal(52 + ((100 - 52) / 2), flagged);
        Assert.True(flagged > unflagged);
    }

    [Fact]
    public void NinetyCapsTheBONUSAndNotTheChance() {
        // A stat at or above ninety skips the bonus entirely and answers itself, so ninety is not a
        // ceiling on the result — treating it as one would quietly cap the best sneaks in the game.
        Assert.Equal(95, CombatEncounterAvoidance.Chance(95, avoidable: false, dragonsBreathActive: false));

        // What IS capped is a bonused sub-ninety stat: 80 would reach 104 unclamped.
        Assert.Equal(90, CombatEncounterAvoidance.Chance(80, avoidable: false, dragonsBreathActive: false));
    }

    [Fact]
    public void OnlyTheFogLiftsASubNinetyStatPastNinety() {
        Assert.Equal(90, CombatEncounterAvoidance.Chance(80, avoidable: true, dragonsBreathActive: false));
        Assert.Equal(95, CombatEncounterAvoidance.Chance(80, avoidable: true, dragonsBreathActive: true));
    }

    [Fact]
    public void ARollEqualToTheChanceStillGetsPast() {
        Assert.True(CombatEncounterAvoidance.Evades(52, 52));
        Assert.False(CombatEncounterAvoidance.Evades(53, 52));
    }

    [Fact]
    public void AWhitelistedEncounterSkipsTheWholeBlock() {
        // Chosen by id rather than by any property of the record, and no amount of Stealth avoids it.
        Assert.True(CombatEncounterAvoidance.AvoidanceIsSkipped(encounterIsWhitelisted: true));
        Assert.Equal(ActorAttribute.Stealth, CombatEncounterAvoidance.Stat);
    }
}
