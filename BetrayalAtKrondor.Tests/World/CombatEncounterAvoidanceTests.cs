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
            avoidable: true, scouted: true, dragonsBreathActive: false, encounterIsWhitelisted: false));
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: true, scouted: false, dragonsBreathActive: false, encounterIsWhitelisted: false));
    }

    [Fact]
    public void TheTwoRoutesIntoTheRollAreDISJOINT() {
        // Scouting works only on flagged encounters, the fog only on unflagged ones. Neither is a
        // general "avoid" mechanic, and a rule that ORs them together lets a scouted party sneak
        // past everything.
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: false, scouted: true, dragonsBreathActive: false, encounterIsWhitelisted: false));
        Assert.True(CombatEncounterAvoidance.MayAttempt(
            avoidable: false, scouted: false, dragonsBreathActive: true, encounterIsWhitelisted: false));
    }

    [Fact]
    public void AWhitelistedEncounterRefusesTheAttemptEvenUnderDragonsBreath() {
        // *** THE WHITELIST HAD NO CALLER UNTIL 2026-09-13. *** AvoidanceIsSkipped was written and
        // tested and never asked, and the list of ids did not exist at all, so the fog let the party
        // walk past six shipped triggers the original never lets anyone avoid.
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: false, scouted: false, dragonsBreathActive: true, encounterIsWhitelisted: true));
        Assert.False(CombatEncounterAvoidance.MayAttempt(
            avoidable: true, scouted: true, dragonsBreathActive: false, encounterIsWhitelisted: true));
    }

    [Fact]
    public void TheWhitelistIsTheOriginalsThirteenIdsAndNotTheReArmingEleven() {
        // HOTSPOT.C:1121-1143. EncounterCompletion.ReArmingEncounters holds eleven of these and is
        // read for a different question; 0x97 and 0x98 appear only here, and they are the two the
        // shipped tile triggers actually reach.
        Assert.Equal(13, CombatEncounterAvoidance.AvoidanceWhitelist.Count);
        foreach (long id in new long[] { 0x97, 0x98, 0xeb, 0xf5, 0x123, 0x125, 0x14f, 0x151, 0x152,
                     0x177, 0x19a, 0x1ad, 0x1ae }) {
            Assert.True(CombatEncounterAvoidance.IsWhitelisted(id), $"0x{id:x} should be whitelisted");
        }

        Assert.False(CombatEncounterAvoidance.IsWhitelisted(2), "the chapter-1 road ambush is not");
        Assert.False(CombatEncounterAvoidance.IsWhitelisted(0x96));
        Assert.False(CombatEncounterAvoidance.IsWhitelisted(0x1af));

        foreach (long id in new long[] { 0x97, 0x98 }) {
            Assert.DoesNotContain(id, EncounterCompletion.ReArmingEncounters);
        }
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
