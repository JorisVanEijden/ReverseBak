namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

/// <summary>
/// The event ids that are game-state FIELDS rather than flags — <c>gstate_event_write</c>.
/// </summary>
public class GameStateEventFieldsTests {
    [Fact]
    public void TheFlagSpaceHasTHREERegionsAndOnlyTwoAreBitmaps() {
        // *** A port that treats the whole space as flags stores the middle writes somewhere
        // nothing reads, and the effect simply does not happen. ***
        Assert.True(GameStateEventFields.IsBitmapFlag(0));
        Assert.True(GameStateEventFields.IsBitmapFlag(GameStateEventFields.LowBitmapLimit - 1));
        Assert.True(GameStateEventFields.IsBitmapFlag(GameStateEventFields.HighBitmapBase));
        Assert.False(GameStateEventFields.IsBitmapFlag(GameStateEventFields.FieldBase));
    }

    [Theory]
    [InlineData(0, GameStateEventFields.Field.EventArgCount)]
    [InlineData(6, GameStateEventFields.Field.ClearLastActionSnapshot)]
    [InlineData(7, GameStateEventFields.Field.Chapter)]
    [InlineData(14, GameStateEventFields.Field.EventArgGoldCost)]
    [InlineData(15, GameStateEventFields.Field.EventArgValue)]
    [InlineData(16, GameStateEventFields.Field.PartyDeathState)]
    [InlineData(17, GameStateEventFields.Field.WorldLoopExitRequest)]
    [InlineData(18, GameStateEventFields.Field.EventArgAuxValue)]
    public void EachMappedOffsetNamesItsField(int offset, GameStateEventFields.Field expected) {
        Assert.Equal(expected, GameStateEventFields.FieldFor(GameStateEventFields.FieldBase + offset));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]   // two shipped dialogs write 30004, and it has NO field
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(13)]
    [InlineData(19)]
    public void TheGAPSAreRealAndFallToTheStub(int offset) {
        // The offsets are sparse. Mapping the range densely would give these a field they do not
        // have — and 30004 is written by shipped content, so the gap is not hypothetical.
        Assert.Equal(GameStateEventFields.Field.None,
            GameStateEventFields.FieldFor(GameStateEventFields.FieldBase + offset));
    }

    [Fact]
    public void ClearingTheLastActionSnapshotIgnoresTheValue() {
        // The arm is `dwLastActionTimeSnapshot = 0;` with the value unread — a RESET, not an
        // assignment. Passing 5 does not make it 5.
        Assert.Equal(0, GameStateEventFields.ValueWritten(
            GameStateEventFields.Field.ClearLastActionSnapshot, 5));
        Assert.Equal(5, GameStateEventFields.ValueWritten(
            GameStateEventFields.Field.Chapter, 5));
    }

    [Fact]
    public void TheChapterIsAnEVENTWRITE_whichIsWhyThereIsNoGoToChapterRoutine() {
        Assert.Equal(GameStateEventFields.Field.Chapter,
            GameStateEventFields.FieldFor(30007));
        Assert.Equal(0x7530, GameStateEventFields.FieldBase);
    }
}
