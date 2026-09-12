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
        Assert.Equal(expected, ProximityScan.ParticipatesInEncounterScan(kind));
    }

    [Fact]
    public void OnlyTheLevelConnectionKindsCanRaiseAnEncounter() {
        // 14, the pit (15), the tunnel (20) and the door (23) — where you arrive or leave.
        Assert.True(ProximityScan.AppearsOnAutomap(0xe));
        Assert.True(ProximityScan.AppearsOnAutomap(0xf));
        Assert.True(ProximityScan.AppearsOnAutomap(0x14));
        Assert.True(ProximityScan.AppearsOnAutomap(0x17));

        // Participating, but never an encounter.
        Assert.True(ProximityScan.ParticipatesInEncounterScan(7));
        Assert.False(ProximityScan.AppearsOnAutomap(7));
    }

    [Fact]
    public void AThresholdOfMinusOneSwitchesTheKindOff() {
        Assert.False(ProximityScan.JoinsEncounterScan(kind: 7, octagonalDistance: 0, radius: 0, shift: 0,
            threshold: ProximityScan.DisabledThreshold, visibleSoFar: 0));
    }

    [Fact]
    public void AThresholdOfOneMeansAlwaysVisible() {
        // The metric is forced to zero rather than compared, so distance stops mattering.
        Assert.Equal(0, ProximityScan.CullingMetric(1_000_000, 0, 0, threshold: 1));
        Assert.True(ProximityScan.JoinsEncounterScan(0, 1_000_000, 0, 0, threshold: 1, visibleSoFar: 0));
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
        Assert.True(ProximityScan.JoinsEncounterScan(7, 4_000, 0, 0, threshold: 5000, visibleSoFar: 0));
        Assert.False(ProximityScan.JoinsEncounterScan(7, 6_000, 0, 0, threshold: 5000, visibleSoFar: 0));
    }

    [Fact]
    public void TheTwoScansDoNotAdmitTheSameKinds() {
        // *** THE ONE LINE THAT DIFFERS BETWEEN proxscan_run AND proxscan_encounter_records. ***
        // The visible list drops kind 7 and takes everything else the filter table allows; the
        // encounter scan keeps 7 and takes only its whitelist. Reading them the wrong way round
        // hides kinds 5, 6, 8, 9, 11-13, 16-19, 21, 22 and 24-42 -- most of the world's furniture --
        // and then draws the db1..db8 records that are never rendered.
        const long anyThreshold = 5000;

        Assert.False(ProximityScan.JoinsVisibleList(7, 0, 0, 0, anyThreshold, 0));
        Assert.True(ProximityScan.JoinsEncounterScan(7, 0, 0, 0, anyThreshold, 0));

        foreach (int kind in new[] { 5, 6, 8, 9, 11, 12, 13, 16, 21, 24, 40, 42 }) {
            Assert.True(ProximityScan.JoinsVisibleList(kind, 0, 0, 0, anyThreshold, 0),
                $"kind {kind} is ordinary furniture and belongs in the visible list");
            Assert.False(ProximityScan.JoinsEncounterScan(kind, 0, 0, 0, anyThreshold, 0),
                $"kind {kind} is not on the encounter scan's whitelist");
        }
    }

    [Fact]
    public void TheVisibleListRespectsTheFilterTableAndTheCap() {
        // Everything the visible list DOES share with the other scan, so a refactor cannot quietly
        // drop one of them: the -1 switch-off, the distance test and the 600-entry cap.
        Assert.False(ProximityScan.JoinsVisibleList(10, 0, 0, 0,
            ProximityScan.DisabledThreshold, 0));
        Assert.True(ProximityScan.JoinsVisibleList(10, 4_000, 0, 0, 5000, 0));
        Assert.False(ProximityScan.JoinsVisibleList(10, 6_000, 0, 0, 5000, 0));
        Assert.False(ProximityScan.JoinsVisibleList(10, 0, 0, 0, 5000,
            ProximityScan.MaxVisibleEntries));
    }

    [Fact]
    public void ABuildingTwoTilesOutIsNOTInTheOriginalsList() {
        // The measurement that opened TASK-436: four houses at octagonal 42,000 to 55,000 with
        // Extent 0, against FILTER.DAT's Building (kind 10) threshold of 17,250 at detail level 0.
        // The original shows sky where we drew them.
        foreach (long distance in new long[] { 42_000, 48_600, 48_700, 55_000 }) {
            Assert.False(ProximityScan.JoinsVisibleList(10, distance, 0, 0, 17_250, 0));
        }
        Assert.True(ProximityScan.JoinsVisibleList(10, 17_249, 0, 0, 17_250, 0));
    }

    [Fact]
    public void TheVisibleListStopsGrowingAtItsCap() {
        Assert.True(ProximityScan.JoinsEncounterScan(7, 0, 0, 0, 5000,
            visibleSoFar: ProximityScan.MaxVisibleEntries - 1));
        Assert.False(ProximityScan.JoinsEncounterScan(7, 0, 0, 0, 5000,
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
        Assert.False(ProximityScan.JoinsEncounterScan(0x17, distance, radius: 0, shift: 0,
            threshold: 1000, visibleSoFar: 0));
    }

    [Fact]
    public void AParticipatingKindThatCannotRaiseAnEncounterNeverDoes() {
        Assert.False(ProximityScan.RecordsOnAutomap(7, 10, Underground, true));
    }
}
