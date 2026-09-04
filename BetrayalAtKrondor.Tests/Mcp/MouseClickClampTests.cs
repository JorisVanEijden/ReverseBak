namespace BetrayalAtKrondor.Tests.Mcp;

using BetrayalAtKrondor.Mcp;
using Xunit;

/// <summary>
/// <c>bak_mouse_click</c> must never place the cursor off the 320x200 screen.
/// </summary>
/// <remarks>
/// <b>An off-screen cursor is destructive, not inert.</b> The tool writes the game's mouse globals
/// directly, bypassing the driver that would clamp them, so it can produce a position real hardware
/// never reports. At <c>y == 200</c>, <c>DrawMouseCursor</c> (IDA 0x2AECF) — which clamps negative
/// coordinates only — derives a clipped cursor height of <c>scrHeight - y == 0</c>, and
/// <c>vga_paste_rect</c>'s do-while row loop turns 0 into 65536 rows, scribbling five 16-pixel bands
/// across the screen.
///
/// <para>That was TASK-315, filed as a rendering blocker and chased through VGA timing, Chain4, the
/// blit, the plane mask and the RNG before the cause turned out to be the repro's own click at
/// (320, 200) — one pixel past the bottom-right corner.</para>
/// </remarks>
public class MouseClickClampTests {
    [Theory]
    [InlineData(320, 200, 319, 199)]   // the exact coordinate that caused TASK-315
    [InlineData(9999, 9999, 319, 199)]
    [InlineData(-1, -1, 0, 0)]
    [InlineData(-50, 250, 0, 199)]
    public void OutOfBoundsIsPulledOntoTheScreen(int x, int y, int wantX, int wantY) {
        Assert.Equal((wantX, wantY), BakMcpTools.ClampToScreen(x, y));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(160, 100)]
    [InlineData(319, 199)]   // the last addressable pixel must survive untouched
    public void InBoundsIsLeftAlone(int x, int y) {
        Assert.Equal((x, y), BakMcpTools.ClampToScreen(x, y));
    }
}
