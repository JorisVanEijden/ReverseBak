namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// wPalEventMask, maintained by spellfx_pal_event_mask_upd on every timer tick. The clear-on-zero
/// behaviour is the part a port loses, because it only happens on the tick before removal.
/// </summary>
public class SpellPaletteEventsTests {
    [Fact]
    public void ARunningEffectSetsItsBit() {
        Assert.Equal(0b0000_0001, SpellPaletteEvents.Apply(0, 0, remainingTime: 5));
        Assert.Equal(0b0001_0000, SpellPaletteEvents.Apply(0, 4, remainingTime: 5));
    }

    [Fact]
    public void ReachingZeroClearsItAgain() {
        int mask = SpellPaletteEvents.Apply(0, 4, remainingTime: 5);

        Assert.Equal(0, SpellPaletteEvents.Apply(mask, 4, remainingTime: 0));
    }

    [Fact]
    public void EffectsDoNotDisturbEachOther() {
        int mask = SpellPaletteEvents.Apply(0, 1, 5);
        mask = SpellPaletteEvents.Apply(mask, 3, 5);

        mask = SpellPaletteEvents.Apply(mask, 1, 0);

        Assert.Equal(SpellPaletteEvents.BitFor(3), mask);
    }

    [Fact]
    public void SettingTheSameEffectTwiceIsIdempotent() {
        int once = SpellPaletteEvents.Apply(0, 2, 5);

        Assert.Equal(once, SpellPaletteEvents.Apply(once, 2, 5));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(99)]
    public void AnOutOfRangeEffectLeavesTheMaskAlone(int eventId) {
        int mask = SpellPaletteEvents.Apply(0, 3, 5);

        Assert.Equal(mask, SpellPaletteEvents.Apply(mask, eventId, 5));
        Assert.Equal(mask, SpellPaletteEvents.Apply(mask, eventId, 0));
        Assert.Equal(0, SpellPaletteEvents.BitFor(eventId));
    }

    [Fact]
    public void TheShippedTableIsExactlyOneShiftedLeft() {
        // g_aPalEventBitMask is written out longhand; it is worth knowing it holds no surprises.
        int[] shipped = { 0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080, 0x0100 };

        for (var id = 0; id < SpellPaletteEvents.Count; id++) {
            Assert.Equal(shipped[id], SpellPaletteEvents.BitFor(id));
        }
    }
}
