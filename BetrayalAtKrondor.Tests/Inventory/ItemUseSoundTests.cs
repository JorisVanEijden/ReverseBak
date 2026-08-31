namespace BetrayalAtKrondor.Tests.Inventory;

using GameData.Resources.Inventory;
using Xunit;

/// <summary>The item's own use cue — <c>itemuse_dispatch_on_target</c>'s tail.</summary>
public class ItemUseSoundTests {
    /// <summary>
    /// THE FENCE. The stored value is EXTRA repeats, so 0 plays once — reading it as a count
    /// silences 27 of the 30 shipped items that carry a sound.
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void TheStoredCountIsEXTRARepeats(int stored, int heard) =>
        Assert.Equal(heard, ItemUseSound.TimesHeard(stored));

    /// <summary>A use that achieved nothing is silent — outcome 0 returns before the cue.</summary>
    [Fact]
    public void NothingHappeningIsSilent() {
        Assert.False(ItemUseSound.Sounds(ItemUseOutcome.NoEffect));
        Assert.False(ItemUseSound.Sounds(ItemUseOutcome.NotPorted));
    }

    /// <summary>
    /// Everything the original reaches the cue with does sound — outcome -2, -1 and 1 all fall past
    /// the outcome-0 early return.
    /// </summary>
    [Fact]
    public void ARealUseSounds() {
        Assert.True(ItemUseSound.Sounds(ItemUseOutcome.Handled));
        Assert.True(ItemUseSound.Sounds(ItemUseOutcome.Silent));
        Assert.True(ItemUseSound.Sounds(ItemUseOutcome.Applied));
    }
}
