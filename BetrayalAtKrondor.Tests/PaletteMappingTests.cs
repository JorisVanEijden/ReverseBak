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
    public void Compass_UsesTravelHudUiPalette() {
        // The travel compass strip (COMPASS.BMX) has no COMPASS.PAL in the archive; it shares the
        // travel-HUD UI palette with the FRAME chrome it sits in.
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("COMPASS.BMX"));
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("COMPASS.BMX", 0));
    }

    [Fact]
    public void Heads_UsesTravelHudUiPalette() {
        // Party portrait heads (HEADS.BMX) have no HEADS.PAL in the archive; addHeads stamps them
        // onto the FRAME chrome, so they share the travel-HUD UI palette with FRAME/COMPASS.
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("HEADS.BMX"));
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("HEADS.BMX", 0));
    }

    [Fact]
    public void ZoneSlotBitmaps_UseZonePalette() {
        // Z##SLOT#.BMX (object/wall textures for Flags&0x10 faces) have no per-slot .PAL; they render
        // under the zone palette, like the Z##L atlas. Covers single- and double-digit zone numbers.
        Assert.Equal("Z01.PAL", PaletteMapping.GetPaletteFor("Z01SLOT3.BMX"));
        Assert.Equal("Z01.PAL", PaletteMapping.GetPaletteFor("Z01SLOT0.BMX", 8));
        Assert.Equal("Z02.PAL", PaletteMapping.GetPaletteFor("Z02SLOT4.BMX"));
        Assert.Equal("Z12.PAL", PaletteMapping.GetPaletteFor("Z12SLOT2.BMX"));
    }

    [Fact]
    public void ZoneAtlas_And_NonSlotZoneNames_Unaffected() {
        // The Z##L atlas rule is unchanged, and a non-SLOT Z-name still falls through to the switch/default.
        Assert.Equal("Z01.PAL", PaletteMapping.GetPaletteFor("Z01L.SCX"));
        Assert.Null(PaletteMapping.GetPaletteFor("Z01.TBL"));
    }

    [Fact]
    public void NonBiconsSubImage_FallsBackToFileMapping() {
        // A sub-image index on a non-overridden file resolves by filename as before.
        Assert.Equal("CONTENTS.PAL", PaletteMapping.GetPaletteFor("CONTENTS.SCX", 0));
        Assert.Equal("OPTIONS.PAL", PaletteMapping.GetPaletteFor("OPTIONS0.SCX", 3));
    }
}
