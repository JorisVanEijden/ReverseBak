namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.GameState;
using GameData.Resources.Scene;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// townscene_load's gating. The chapter list is a HIDE list and the flag gate is a RANGE — both are
/// easy to invert, and inverting either makes interactions appear in the wrong chapter or vanish.
/// </summary>
public class GdsSceneRulesTests {
    private static GdsHotspot Hotspot(List<int> hiddenInChapters = null, Condition gate = null) =>
        new GdsHotspot {
            HiddenInChapters = hiddenInChapters ?? new List<int>(),
            VisibilityGate = gate,
        };

    [Fact]
    public void AnUngatedHotspotIsVisible() {
        Assert.True(GdsSceneRules.IsHotspotVisible(Hotspot(), chapter: 1, preserve: false, null));
    }

    [Fact]
    public void TheChapterListHidesRatherThanShows() {
        GdsHotspot hotspot = Hotspot(hiddenInChapters: new List<int> { 2, 3 });

        Assert.True(GdsSceneRules.IsHotspotVisible(hotspot, 1, false, null));
        Assert.False(GdsSceneRules.IsHotspotVisible(hotspot, 2, false, null));
        Assert.False(GdsSceneRules.IsHotspotVisible(hotspot, 3, false, null));
        Assert.True(GdsSceneRules.IsHotspotVisible(hotspot, 4, false, null));
    }

    [Fact]
    public void APreservedLoadIgnoresTheChapterListEntirely() {
        // Re-entering a scene creates every hotspot regardless of chapter; only the flag gate still
        // applies. Getting this wrong makes hotspots vanish when you step back into a location.
        GdsHotspot hotspot = Hotspot(hiddenInChapters: new List<int> { 2 });

        Assert.False(GdsSceneRules.IsHotspotVisible(hotspot, 2, preserve: false, null));
        Assert.True(GdsSceneRules.IsHotspotVisible(hotspot, 2, preserve: true, null));
    }

    [Fact]
    public void AFailedGateHidesTheHotspotEvenInAnAllowedChapter() {
        GdsHotspot hotspot = Hotspot(gate: new VarCondition { Var = 12, Min = 2, Max = 8 });

        Assert.False(GdsSceneRules.IsHotspotVisible(hotspot, 1, false, _ => false));
        Assert.True(GdsSceneRules.IsHotspotVisible(hotspot, 1, false, _ => true));
    }

    [Fact]
    public void APreservedLoadStillHonoursTheGate() {
        GdsHotspot hotspot = Hotspot(hiddenInChapters: new List<int> { 2 },
            gate: new VarCondition { Var = 12, Min = 2, Max = 8 });

        Assert.False(GdsSceneRules.IsHotspotVisible(hotspot, 2, preserve: true, _ => false));
    }

    [Fact]
    public void HotspotActionIdsStartAtOneTwentyEight() {
        Assert.Equal(0x80, GdsSceneRules.ActionIdFor(0));
        Assert.Equal(0x85, GdsSceneRules.ActionIdFor(5));
    }

    [Fact]
    public void ASceneNormallyOpensOnTheSubYouAskedFor() {
        Assert.Equal(3, GdsSceneRules.ResolveSubScene(chapter: 5, sub: 3, _ => 1));
    }

    [Fact]
    public void TwoLocationsSwapThemselvesOnceAStoryFlagIsSet() {
        // The redirect happens before the filename is built, so the scene loaded is not the one
        // requested. Both pairs are hard-coded rather than data, so they cannot come from the GDS.
        Assert.Equal(7, GdsSceneRules.ResolveSubScene(0x40, 1, flag => flag == 0x1c86 ? 1 : 0));
        Assert.Equal(4, GdsSceneRules.ResolveSubScene(1, 1, flag => flag == 0x7539 ? 1 : 0));
    }

    [Fact]
    public void TheRedirectsDoNotFireWithTheFlagClear() {
        Assert.Equal(1, GdsSceneRules.ResolveSubScene(0x40, 1, _ => 0));
        Assert.Equal(1, GdsSceneRules.ResolveSubScene(1, 1, _ => 0));
    }

    [Fact]
    public void TheRedirectsAreTiedToTheirOwnChapterAndSub() {
        Assert.Equal(2, GdsSceneRules.ResolveSubScene(1, 2, _ => 1));   // right chapter, wrong sub
        Assert.Equal(1, GdsSceneRules.ResolveSubScene(2, 1, _ => 1));   // right sub, wrong chapter
    }

    [Theory]
    [InlineData(10, 1, 10)]    // chapter 1 pays the base rate: 10*20/20
    [InlineData(10, 5, 12)]    // 10*24/20
    [InlineData(10, 9, 14)]    // 10*28/20
    public void InnsGetDearerAsTheStoryAdvances(int baseUnit, int chapter, int expected) {
        Assert.Equal(expected, GdsSceneRules.InnNightlyRate(baseUnit, chapter));
    }

    [Fact]
    public void TheNightlyRateIsCapped() {
        Assert.Equal(0xfa, GdsSceneRules.InnNightlyRate(baseUnit: 250, chapter: 9));
    }

    [Fact]
    public void TheCdBuildClimbsHalfAsFastAsTheFloppy() {
        // Floppy is (chapter + 9)/10; we target the CD's (chapter + 19)/20. At chapter 9 the floppy
        // would charge 18 against our 14 for the same base — taking the wrong branch overcharges.
        int ours = GdsSceneRules.InnNightlyRate(10, 9);
        int floppy = 10 * (9 + 9) / 10;

        Assert.Equal(14, ours);
        Assert.Equal(18, floppy);
    }
}
