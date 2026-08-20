namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// proxscan_encounter_records — the per-move scan that builds the visible list and fires the
/// roaming-encounter check. The two tests it runs side by side use DIFFERENT distances, which is
/// the thing to get right.
/// </summary>
public class ProximityScanTests {
    private const int Underground = 2;
    private const int Outdoors = 1;

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    [InlineData(0xf, true)]
    [InlineData(0x27, true)]
    [InlineData(0x28, false)]
    public void OnlyCertainKindsAreScannedAtAll(int kind, bool expected) {
        Assert.Equal(expected, ProximityScan.Participates(kind));
    }

    [Fact]
    public void OnlyTheLevelConnectionKindsCanRaiseAnEncounter() {
        // 14, the pit (15), the tunnel (20) and the door (23) — where you arrive or leave.
        Assert.True(ProximityScan.AppearsOnAutomap(0xe));
        Assert.True(ProximityScan.AppearsOnAutomap(0xf));
        Assert.True(ProximityScan.AppearsOnAutomap(0x14));
        Assert.True(ProximityScan.AppearsOnAutomap(0x17));

        // Participating, but never an encounter.
        Assert.True(ProximityScan.Participates(7));
        Assert.False(ProximityScan.AppearsOnAutomap(7));
    }

    [Fact]
    public void AThresholdOfMinusOneSwitchesTheKindOff() {
        Assert.False(ProximityScan.IsVisible(kind: 7, octagonalDistance: 0, radius: 0, shift: 0,
            threshold: ProximityScan.DisabledThreshold, visibleSoFar: 0));
    }

    [Fact]
    public void AThresholdOfOneMeansAlwaysVisible() {
        // The metric is forced to zero rather than compared, so distance stops mattering.
        Assert.Equal(0, ProximityScan.CullingMetric(1_000_000, 0, 0, threshold: 1));
        Assert.True(ProximityScan.IsVisible(0, 1_000_000, 0, 0, threshold: 1, visibleSoFar: 0));
    }

    [Fact]
    public void ABiggerObjectRegistersFromFurtherAway() {
        // The test is against the entity's edge: radius << shift comes off the distance first.
        long small = ProximityScan.CullingMetric(10_000, radius: 10, shift: 0, threshold: 5000);
        long large = ProximityScan.CullingMetric(10_000, radius: 10, shift: 8, threshold: 5000);

        Assert.Equal(9_990, small);
        Assert.Equal(10_000 - (10 << 8), large);
        Assert.True(large < small);
    }

    [Fact]
    public void AnEntityIsVisibleOnlyInsideItsThreshold() {
        Assert.True(ProximityScan.IsVisible(7, 4_000, 0, 0, threshold: 5000, visibleSoFar: 0));
        Assert.False(ProximityScan.IsVisible(7, 6_000, 0, 0, threshold: 5000, visibleSoFar: 0));
    }

    [Fact]
    public void TheVisibleListStopsGrowingAtItsCap() {
        Assert.True(ProximityScan.IsVisible(7, 0, 0, 0, 5000,
            visibleSoFar: ProximityScan.MaxVisibleEntries - 1));
        Assert.False(ProximityScan.IsVisible(7, 0, 0, 0, 5000,
            visibleSoFar: ProximityScan.MaxVisibleEntries));
    }

    // ---- the encounter check ---------------------------------------------------------------

    [Fact]
    public void RoamingEncountersAreCheckedUndergroundOnly() {
        Assert.True(ProximityScan.RecordsOnAutomap(0xf, 100, Underground, true));
        Assert.False(ProximityScan.RecordsOnAutomap(0xf, 100, Outdoors, true));
    }

    [Fact]
    public void TheEncounterRangeIsFixedAtSixteenHundred() {
        Assert.True(ProximityScan.RecordsOnAutomap(0xf, 0x63f, Underground, true));
        Assert.False(ProximityScan.RecordsOnAutomap(0xf, 0x640, Underground, true));
    }

    [Fact]
    public void NoEncounterTableMeansNoCheck() {
        Assert.False(ProximityScan.RecordsOnAutomap(0xf, 100, Underground, hasAutomapRecord: false));
    }

    [Fact]
    public void TheEncounterCheckIgnoresTheEntitysSize() {
        // Deliberately different from the visibility test beside it: the encounter range is measured
        // on the RAW distance, so a large door and a small one trigger at the same range.
        const long distance = 0x600;
        Assert.True(ProximityScan.RecordsOnAutomap(0x17, distance, Underground, true));

        // The same entity may well be culled from the visible list at that distance.
        Assert.False(ProximityScan.IsVisible(0x17, distance, radius: 0, shift: 0,
            threshold: 1000, visibleSoFar: 0));
    }

    [Fact]
    public void AParticipatingKindThatCannotRaiseAnEncounterNeverDoes() {
        Assert.False(ProximityScan.RecordsOnAutomap(7, 10, Underground, true));
    }
}
