namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using GameData.Resources.Data;
using Xunit;

/// <summary>
/// Disarming a trapped chest — the <c>trapped</c> arm of <c>handle_Container</c> @0x77284.
/// </summary>
/// <remarks>
/// <b>These were written against a second model of the same mechanic.</b> ChestTrapDisarm and
/// ChestTrap both described this branch, with the same dialog ids and the same rules under
/// different names — and only ChestTrap had a caller. The duplicate is gone; these tests moved
/// onto the survivor rather than being deleted with it.
///
/// <para>The two <c>Succeeds</c> functions took the SAME comparison with REVERSED parameter
/// order, which is the part worth remembering: <c>lockScore &lt; best</c> against
/// <c>best &gt; difficulty</c>. Calling the wrong one positionally inverts the outcome silently,
/// and nothing about the names would warn you.</para>
/// </remarks>
public class ChestTrapDisarmTests {
    [Fact]
    public void TheDisarmIsOfferedONLYWithDetectionAndARealTrap() {
        // Both conditions, not either. A chest disarmed earlier carries zero damage and stops
        // re-offering, which is the same test doing double duty.
        Assert.True(ChestTrap.Detected(detectionSpellActive: true, trapDamage: 20));
        Assert.False(ChestTrap.Detected(detectionSpellActive: false, trapDamage: 20));
        Assert.False(ChestTrap.Detected(detectionSpellActive: true, trapDamage: 0));
    }

    [Fact]
    public void SuccessIsSTRICT_AndThereIsNoRoll() {
        // difficulty >= best FAILS, so an exact tie loses and the same party either can or cannot
        // disarm a given chest every time. A die here turns a fixed obstacle into a save-scummable
        // one.
        Assert.True(ChestTrap.DisarmSucceeds(bestLockpicking: 41, difficulty: 40));
        Assert.False(ChestTrap.DisarmSucceeds(bestLockpicking: 40, difficulty: 40));
        Assert.False(ChestTrap.DisarmSucceeds(bestLockpicking: 39, difficulty: 40));
    }

    [Fact]
    public void TheAwardISThePicklockAward_NotACoincidentalTwo() {
        // Pointed at the constant so the two cannot drift; the disarm is deliberately the same
        // shape as picking a lock and shares its reward.
        Assert.Equal(PicklockAttempt.SkillOnSuccess, ChestTrap.DisarmSkillAward);
    }

    [Fact]
    public void FailingSaysNOTHING() {
        // The player finds out by opening the chest. A "you failed" line would give away for free
        // what the detection spell is for.
        Assert.False(ChestTrap.AnnouncesFailure);
    }

    [Fact]
    public void TheThreeDialogsAreDistinctQuestions() {
        Assert.NotEqual(ChestTrap.DetectedPromptDialog, ChestTrap.DisarmedDialog);
        Assert.NotEqual(ChestTrap.OpenStillTrappedDialog, ChestTrap.OpenExTrappedDialog);
    }
}
