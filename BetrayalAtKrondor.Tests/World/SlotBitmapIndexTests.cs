namespace BetrayalAtKrondor.Tests.World;

using global::GameData.Resources.World;
using global::ResourceExtraction.World;
using Xunit;

public class SlotBitmapIndexTests {
    // Z01 slot image counts.
    private static ZoneSlotBitmapIndex Z01() => new(new[] { 6, 16, 4, 13, 14 });

    [Theory]
    [InlineData(27, 3, 1)]   // chest body-set → SLOT3 img1
    [InlineData(34, 3, 8)]   // chest body → SLOT3 img8
    [InlineData(22, 2, 0)]   // bridge → SLOT2 img0
    [InlineData(0, 0, 0)]
    [InlineData(38, 3, 12)]  // last SLOT3 image
    [InlineData(39, 4, 0)]   // first SLOT4 image
    public void Resolves_global_index_to_slot_and_local(int global, int slot, int local) {
        Assert.True(Z01().TryResolve(global, out var r));
        Assert.Equal(new SlotBitmapRef(slot, local), r);
    }

    [Theory]
    [InlineData(53)]   // one past the end (6+16+4+13+14)
    [InlineData(-1)]
    public void Out_of_range_returns_false(int global) {
        Assert.False(Z01().TryResolve(global, out _));
    }
}
