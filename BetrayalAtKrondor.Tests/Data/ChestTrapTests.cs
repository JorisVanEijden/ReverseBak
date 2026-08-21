namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Trapped chests: what the party is told, and what disarming costs.
/// </summary>
public class ChestTrapTests {
    [Fact]
    public void WITHOUTTHESPELLTHEREISNOWARNINGATALL() {
        // *** The detection mechanic IS the spell. *** An unprotected party is simply asked whether
        // to open the chest, with no hint anything is wrong — so a port that always offers a disarm
        // gives away information the game charges a spell for.
        Assert.False(ChestTrap.Detected(detectionSpellActive: false, trapDamage: 40));
        Assert.True(ChestTrap.Detected(detectionSpellActive: true, trapDamage: 40));
    }

    [Fact]
    public void ADisarmedChestIsNotDetectedAgain() {
        // Zero trap damage is the "dealt with" state, so the warning does not reappear.
        Assert.False(ChestTrap.Detected(detectionSpellActive: true, trapDamage: 0));
    }

    [Fact]
    public void THEDISARMISDETERMINISTICANDSTRICT() {
        // difficulty >= best FAILS, so an exact tie loses — and there is no roll, so re-trying the
        // same chest with the same party can never succeed. Adding a die makes a fixed obstacle
        // save-scummable.
        Assert.True(ChestTrap.DisarmSucceeds(bestLockpicking: 51, difficulty: 50));
        Assert.False(ChestTrap.DisarmSucceeds(bestLockpicking: 50, difficulty: 50));
        Assert.False(ChestTrap.DisarmSucceeds(bestLockpicking: 49, difficulty: 50));
    }

    [Fact]
    public void AFAILEDDISARMDOESNOTSPRINGTHETRAP() {
        // It falls through to exactly the prompt an UNDETECTED trap shows, so failing costs the
        // attempt and nothing else — the player is still asked before anything happens.
        Assert.Equal(ChestTrap.OpenStillTrappedDialog, ChestTrap.OpenPromptFor(trapDamage: 40));
    }

    [Fact]
    public void ADefusedChestStillAsks_WithItsOwnLine() {
        // Easy to drop on the grounds that there is nothing left to fear, but the prompt is what the
        // player expects after the effort of disarming it — and it is a different line.
        Assert.Equal(ChestTrap.OpenExTrappedDialog, ChestTrap.OpenPromptFor(trapDamage: 0));
        Assert.NotEqual(ChestTrap.OpenStillTrappedDialog, ChestTrap.OpenExTrappedDialog);
    }

    [Fact]
    public void DisarmingTrainsTheActorWhoDidIt_AndOnlyOnSuccess() {
        Assert.Equal(2, ChestTrap.DisarmSkillAward);
        Assert.Equal(13, ChestTrap.DisarmAttribute);
    }

    [Fact]
    public void ADisarmIsPermanentStateOnTheContainer() {
        // Success zeroes the record's trap damage, so the chest is safe on the next visit too. A
        // session flag would re-arm it.
        Assert.True(ChestTrap.DisarmIsPermanent);
    }

    [Fact]
    public void TheDetectionSpellIsScentOfSarig() {
        Assert.Equal(FieldSpells.ScentOfSarig, ChestTrap.DetectionSpell);
    }
}
