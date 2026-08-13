namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// picklock_screen_run's shared rules. The band edges and what "mode" actually is are the two
/// things worth pinning.
/// </summary>
public class LockPickingTests {
    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 1)]     // 0x32 exactly — still tier 1
    [InlineData(51, 2)]
    [InlineData(80, 2)]     // 0x50 exactly — still tier 2
    [InlineData(81, 3)]
    [InlineData(100, 3)]    // 0x64 exactly — still tier 3
    [InlineData(101, 4)]
    [InlineData(255, 4)]
    public void TheDifficultyBandsAreOpenAtTheBottom(int score, int expectedTier) {
        // Every test is a strict "greater than", so a threshold is the first score of the tier
        // ABOVE it, not the last of its own.
        Assert.Equal(expectedTier, LockPicking.DifficultyTier(score));
    }

    [Fact]
    public void TheTiersRunOneToFour() {
        Assert.Equal(1, LockPicking.DifficultyTier(int.MinValue));
        Assert.Equal(4, LockPicking.DifficultyTier(int.MaxValue));
    }

    [Fact]
    public void TheContextIsWhichLockNotHowHard() {
        // It reaches the DDX as an event argument and only picks the wording. Distinct values, no
        // ordering meaning.
        Assert.Equal(0, (int)LockPicking.LockContext.Person);
        Assert.Equal(1, (int)LockPicking.LockContext.Door);
        Assert.Equal(2, (int)LockPicking.LockContext.Container);
        Assert.Equal(3, (int)LockPicking.LockContext.Traversal);
    }

    [Fact]
    public void APartyWithLockpicksCanAlwaysTry() {
        Assert.True(LockPicking.CanAttempt(sharedItemCount: 0, lockpickCount: 3));
    }

    [Fact]
    public void APartyWithOtherItemsButNoPicksStillGetsTheScreen() {
        // The original only refuses when the working inventory comes to nothing, so a party with
        // shared items but no picks opens the screen and simply cannot succeed at it.
        Assert.True(LockPicking.CanAttempt(sharedItemCount: 4, lockpickCount: 0));
    }

    [Fact]
    public void APartyWithNothingAtAllIsRefused() {
        Assert.False(LockPicking.CanAttempt(sharedItemCount: 0, lockpickCount: 0));
    }

    [Fact]
    public void ManyLockpicksStillCountAsOneStack() {
        // They are appended as a single slot whose condition is the count, so the item total goes
        // up by one however many the party carries.
        Assert.Equal(LockPicking.CanAttempt(0, 1), LockPicking.CanAttempt(0, 99));
    }

    [Fact]
    public void ThePickerIsThePartysBestLockpick() {
        Assert.Equal(ActorAttribute.LockPicking, LockPicking.PickerAttribute);
    }
}
