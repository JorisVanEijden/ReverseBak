namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

/// <summary>
/// Where a global flag lives — and why the high bitmap is not one more linear bitfield.
/// </summary>
public class GlobalFlagLayoutTests {
    [Fact]
    public void TheHIGHMapPacksTenFlagsPerByteFromAWRAPPEDSum() {
        // 56000 + 0x2540 = 65536, which wraps to 0 in 16 bits. Read in 32 bits the row would be
        // 6553 — far outside the fifty bytes the block has.
        Assert.True(GlobalFlagLayout.TryHighPosition(56001, out int row, out int bit));
        Assert.Equal(0, row);
        Assert.Equal(0, bit);

        Assert.True(GlobalFlagLayout.TryHighPosition(56013, out row, out bit));
        Assert.Equal(1, row);
        Assert.Equal(2, bit);
    }

    [Fact]
    public void THEWRAPISLOADBEARING_thirtyTwoBitArithmeticGoesOutOfTheBlock() {
        // The highest id the shipped dialogs use. In 16 bits it is row 40 of 50; unwrapped it would
        // be row 6594.
        Assert.True(GlobalFlagLayout.TryHighPosition(56404, out int row, out int bit));
        Assert.Equal(40, row);
        Assert.Equal(3, bit);
        Assert.True(row < GlobalFlagLayout.HighByteCount);

        int unwrapped = (56404 + GlobalFlagLayout.HighBias) / GlobalFlagLayout.HighFlagsPerByte;
        Assert.True(unwrapped > GlobalFlagLayout.HighByteCount,
            "which is exactly why the wrap cannot be dropped");
    }

    [Fact]
    public void TheLinearReadingNEEDSMOREBYTESTHANTHEBLOCKHAS() {
        // `key - 56000` for the highest shipped id is bit 404, i.e. byte 50 of a 50-byte block.
        // One of the two structural arguments that the linear reading was wrong.
        const int highestShipped = 56404;
        int linearByte = (highestShipped - 56000) / 8;
        Assert.True(linearByte >= GlobalFlagLayout.HighByteCount);
    }

    [Fact]
    public void TheTwoUNADDRESSABLEPositionsAreRefused_notGuessed() {
        // cx % 10 == 0 gives bit -1 (`1 << -1`, undefined in C and differently wrong in C#);
        // cx % 10 == 9 gives bit 8, which no byte can hold — the original's read is always 0 and
        // its write is lost to the truncation. NO SHIPPED FLAG REACHES EITHER.
        Assert.False(GlobalFlagLayout.TryHighPosition(56000, out _, out _));  // cx=0     -> bit -1
        Assert.False(GlobalFlagLayout.TryHighPosition(56009, out _, out _));  // cx=9     -> bit 8
    }

    [Fact]
    public void ReadingPullsTheRightBitOutOfTheRightByte() {
        var block = new byte[GlobalFlagLayout.HighByteCount];
        block[1] = 0b0000_0100;   // row 1, bit 2 -> id 56013

        Assert.True(GlobalFlagLayout.TryReadHigh(block, 56013, out int set));
        Assert.Equal(1, set);

        Assert.True(GlobalFlagLayout.TryReadHigh(block, 56012, out int clear));
        Assert.Equal(0, clear);
    }

    [Fact]
    public void AnIdWithNoAddressablePositionReadsAsAbsent() {
        var block = new byte[GlobalFlagLayout.HighByteCount];
        Assert.False(GlobalFlagLayout.TryReadHigh(block, 56000, out _));
        Assert.False(GlobalFlagLayout.TryReadHigh(null, 56013, out _));
    }

    [Fact]
    public void TheLowMapIsTheORDINARYLinearOne() {
        // Stated so the asymmetry is deliberate rather than an oversight: only the high map is odd.
        Assert.True(GlobalFlagLayout.IsLowFlag(8127));
        Assert.False(GlobalFlagLayout.IsLowFlag(GlobalFlagLayout.LowLimit));
        Assert.False(GlobalFlagLayout.IsHighFlag(30007));
    }
}
