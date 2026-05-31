namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;

using ResourceExtraction.Extractors;

using Xunit;

/// <summary>
/// Verifies the world-map marker derived from a save header. The header stores the party pin
/// as pixel coords on the 320x200 FULLMAP.SCX plus an icon; the original game adds 2 to the
/// icon on load and hides the marker when worldX is the -1 sentinel (StartGameOrLoadSave 0x20835).
/// Reference values are the real SAVE00.GAM / SAVE01.GAM headers.
/// </summary>
public class SaveGameMarkerTests {
    [Fact]
    public void BuildFullMapMarker_Save00Header_NormalisesAndBiasesIcon() {
        FullMapIcon marker = SaveGameExtractor.BuildFullMapMarker(worldX: 108, worldY: 90, mapIcon: 20);

        Assert.True(marker.Visible);
        Assert.Equal(108f / 320f * 100f, marker.XPercent, 3);
        Assert.Equal(90f / 200f * 100f, marker.YPercent, 3);
        Assert.Equal(22, marker.IconIndex); // 20 + 2 load bias
    }

    [Fact]
    public void BuildFullMapMarker_Save01Header_NormalisesAndBiasesIcon() {
        FullMapIcon marker = SaveGameExtractor.BuildFullMapMarker(worldX: 114, worldY: 94, mapIcon: 12);

        Assert.True(marker.Visible);
        Assert.Equal(114f / 320f * 100f, marker.XPercent, 3);
        Assert.Equal(94f / 200f * 100f, marker.YPercent, 3);
        Assert.Equal(14, marker.IconIndex); // 12 + 2 load bias
    }

    [Fact]
    public void BuildFullMapMarker_MinusOneWorldX_IsNotVisible() {
        FullMapIcon marker = SaveGameExtractor.BuildFullMapMarker(worldX: -1, worldY: 50, mapIcon: 5);

        Assert.False(marker.Visible);
    }
}
