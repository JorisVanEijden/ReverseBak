namespace BetrayalAtKrondor.Tests;

using GameData;

using Xunit;

public class PaletteMappingTests {
    [Fact]
    public void ContentsExitIcon_SubImage66_UsesContentsPalette() {
        // The Contents-screen Exit button graphic is authored for CONTENTS.PAL, not the shared OPTIONS
        // UI palette: normal frame = BICONS1 sub-image 66, hover frame = BICONS2 sub-image 66.
        Assert.Equal("CONTENTS.PAL", PaletteMapping.GetPaletteFor("BICONS1.BMX", 66));
        Assert.Equal("CONTENTS.PAL", PaletteMapping.GetPaletteFor("BICONS2.BMX", 66));
    }

    [Fact]
    public void Bicons_OtherSubImages_KeepOptionsPalette() {
        // Every other BICONS frame stays on the shared OPTIONS UI palette.
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("BICONS1.BMX", 10));
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("BICONS2.BMX", 0));
    }

    [Fact]
    public void Bicons_WithoutSubImage_KeepOptionsPalette() {
        // The default (no sub-image supplied) path is unchanged for existing callers.
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("BICONS1.BMX"));
    }

    [Fact]
    public void NonBiconsSubImage_FallsBackToFileMapping() {
        // A sub-image index on a non-overridden file resolves by filename as before.
        Assert.Equal("CONTENTS.PAL", PaletteMapping.GetPaletteFor("CONTENTS.SCX", 0));
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("OPTIONS0.SCX", 3));
    }
}
