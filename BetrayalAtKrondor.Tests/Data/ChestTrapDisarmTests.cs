namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// Disarming a trapped chest (<c>handle_Container</c> @0x77377).
/// </summary>
public class ChestTrapDisarmTests {
    [Fact]
    public void WithTheSpellAndATrapTheDisarmIsOffered() =>
        Assert.True(ChestTrapDisarm.IsOffered(scentOfSarigActive: true, trapDamage: 20));

    [Fact]
    public void WithoutTheSpellTheTrapIsNeverMentioned() =>
        // Not merely "no disarm option" — the player is given no hint the chest is trapped at all,
        // and gets the blunt open-anyway question instead. That is what the spell is for.
        Assert.False(ChestTrapDisarm.IsOffered(scentOfSarigActive: false, trapDamage: 20));

    [Fact]
    public void AnAlreadyDisarmedChestStopsOffering() =>
        // Zero trap damage is how a chest disarmed earlier remembers; both conditions are required.
        Assert.False(ChestTrapDisarm.IsOffered(scentOfSarigActive: true, trapDamage: 0));

    [Fact]
    public void DisarmingSucceedsBelowThePartysBestSkill() =>
        Assert.True(ChestTrapDisarm.Succeeds(lockScore: 40, bestPartyLockPicking: 41));

    [Fact]
    public void EqualSkillIsNotEnough() =>
        // The original branches to failure on difficulty >= skill, so exactly-equal fails. An
        // implementation using >= for success opens chests the original does not.
        Assert.False(ChestTrapDisarm.Succeeds(lockScore: 40, bestPartyLockPicking: 40));

    [Fact]
    public void TheOutcomeIsDeterministic() {
        // No roll: the same party either can or cannot disarm a given chest, every time.
        for (var i = 0; i < 50; i++) {
            Assert.True(ChestTrapDisarm.Succeeds(50, 51));
            Assert.False(ChestTrapDisarm.Succeeds(50, 50));
        }
    }

    [Fact]
    public void TheRewardIsTheSameOnePickingALockGives() =>
        // Shared deliberately — this is the lock-picking mechanic applied to a trap, not a second
        // system with its own economy.
        Assert.Equal(PicklockAttempt.SkillOnSuccess, ChestTrapDisarm.SkillOnSuccess);

    [Fact]
    public void FailureSaysNothing() =>
        Assert.False(ChestTrapDisarm.AnnouncesFailure);
}
