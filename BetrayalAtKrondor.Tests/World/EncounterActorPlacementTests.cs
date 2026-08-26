namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.Data;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Putting one encounter actor on the field.
/// </summary>
public class EncounterActorPlacementTests {
    private const int TileX = 4;
    private const int TileY = 9;

    private static EnemySlot Template() => new EnemySlot {
        CreatureNumber = 17,
        PrimarySpawnX = 5000, PrimarySpawnY = 6000, PrimaryRotationZ = 0x1234,
        MovementPattern = (ushort)RoamingMovement.Pattern.BackAndForth,
        AltSpawnX = new[] { 1000, 2000, 3000, 4000 },
        AltSpawnY = new[] { 1100, 2200, 3300, 4400 },
    };

    private static EncounterObjectStates.Entry Pose(int x, int y, short facing) =>
        new EncounterObjectStates.Entry {
            WorldXOffset = x, WorldYOffset = y, Facing = facing,
        };

    private static bool Place(int stateWord, out EncounterActorPlacement.Placed placed,
        out int after, bool standingOnly = false, EncounterObjectStates.Entry? stored = null,
        int frameRoll = 0, int directionRoll = 0) =>
        EncounterActorPlacement.TryPlace(stateWord, standingOnly, Template(),
            stored ?? Pose(1000, 2000, 0x0777), TileX, TileY, frameRoll, directionRoll,
            out placed, out after);

    [Fact]
    public void APendingActorTakesTheRecordsAuthoredSpawn() {
        Assert.True(Place(EncounterActorSpawn.Pending, out EncounterActorPlacement.Placed p, out _));

        Assert.Equal((TileX * (long)WorldPlacement.TileSize) + 5000, p.WorldX);
        Assert.Equal((TileY * (long)WorldPlacement.TileSize) + 6000, p.WorldY);
        Assert.Equal(0x1234, p.Facing);
        Assert.Equal(17, p.CreatureNumber);
    }

    [Fact]
    public void AnAlreadyPlacedActorRESUMESFromItsStoredPose() {
        // *** THE DISCRIMINATING CASE. *** Taking the template every time snaps a roamer back to its
        // authored post whenever the party re-enters the chunk, which reads as a monster that never
        // moves. The stored pose is different from the template on purpose here.
        Assert.True(Place(EncounterActorSpawn.Roaming, out EncounterActorPlacement.Placed p, out _));

        Assert.Equal((TileX * (long)WorldPlacement.TileSize) + 1000, p.WorldX);
        Assert.Equal((TileY * (long)WorldPlacement.TileSize) + 2000, p.WorldY);
        Assert.Equal(0x0777, p.Facing);
    }

    [Fact]
    public void StandingResumesToo_AndOnlyRoamingReportsThatItWalks() {
        Assert.True(Place(EncounterActorSpawn.Standing, out EncounterActorPlacement.Placed standing, out _));
        Assert.Equal((TileX * (long)WorldPlacement.TileSize) + 1000, standing.WorldX);
        Assert.False(standing.Roams);

        Assert.True(Place(EncounterActorSpawn.Roaming, out EncounterActorPlacement.Placed roaming, out _));
        Assert.True(roaming.Roams);
    }

    [Fact]
    public void GoneAndUnseededPutNothingOnTheField() {
        Assert.False(Place(EncounterActorSpawn.Gone, out _, out _));
        Assert.False(Place(EncounterActorSpawn.Unseeded, out _, out _));
    }

    [Fact]
    public void TheStandingOnlyFlagNarrowsItToStandingAlone() {
        Assert.False(Place(EncounterActorSpawn.Roaming, out _, out _, standingOnly: true));
        Assert.False(Place(EncounterActorSpawn.Pending, out _, out _, standingOnly: true));
        Assert.True(Place(EncounterActorSpawn.Standing, out _, out _, standingOnly: true));
    }

    [Fact]
    public void ONLYAPendingActorChangesItsStateWord() {
        // A caller that wrote the word back unconditionally would rewrite entries it never touched.
        Place(EncounterActorSpawn.Pending, out _, out int afterPending, frameRoll: 2, directionRoll: 1);
        Assert.NotEqual(EncounterActorSpawn.Pending, afterPending);
        Assert.Equal(EncounterActorSpawn.KindOf(EncounterActorSpawn.Roaming),
            EncounterActorSpawn.KindOf(afterPending));

        Place(EncounterActorSpawn.Roaming, out _, out int afterRoaming);
        Assert.Equal(EncounterActorSpawn.Roaming, afterRoaming);

        Place(EncounterActorSpawn.Standing, out _, out int afterStanding);
        Assert.Equal(EncounterActorSpawn.Standing, afterStanding);
    }

    [Fact]
    public void AFreshlyPlacedActorStartsMidStrideAndWALKS() {
        // It becomes roaming in the same breath as being placed, so the caller does not have to
        // notice the promotion to know it moves.
        Assert.True(Place(EncounterActorSpawn.Pending, out EncounterActorPlacement.Placed p,
            out int after, frameRoll: 1, directionRoll: 0));

        Assert.True(p.Roams);
        Assert.Equal(1, after & 0x03);
        Assert.Equal(0, after & EncounterActorSpawn.WalkDirectionBit);
    }

    [Fact]
    public void TheOriginIsThePARTYSTile_NotTheWorldOrigin() {
        // From tile 0 the two coincide and every reading passes.
        Assert.True(EncounterActorPlacement.TryPlace(EncounterActorSpawn.Pending, false, Template(),
            Pose(0, 0, 0), 0, 0, 0, 0, out EncounterActorPlacement.Placed atOrigin, out _));
        Assert.True(Place(EncounterActorSpawn.Pending, out EncounterActorPlacement.Placed away, out _));

        Assert.Equal(5000, atOrigin.WorldX);
        Assert.Equal(away.WorldX - atOrigin.WorldX, TileX * (long)WorldPlacement.TileSize);
    }

    [Fact]
    public void TheWaypointsAreREBASEDOntoThePartysTile() {
        // *** THE WHOLE REASON THE ARRIVAL TEST WORKS. *** The record stores them tile-relative and
        // the updater compares them against the ABSOLUTE position by exact equality. Left relative,
        // the comparison can only match in tile (0,0) and every patrol elsewhere walks off forever.
        Assert.True(Place(EncounterActorSpawn.Pending, out EncounterActorPlacement.Placed p, out _));

        long originX = TileX * (long)WorldPlacement.TileSize;
        long originY = TileY * (long)WorldPlacement.TileSize;
        Assert.Equal(originX + 1000, p.WaypointX[0]);
        Assert.Equal(originY + 1100, p.WaypointY[0]);
        Assert.Equal(originX + 2000, p.WaypointX[1]);
    }

    [Fact]
    public void ONLYTheWaypointsThePatternReadsAreCarried() {
        // Back-and-forth reads two of the four. The other two are junk on most shipped slots — for
        // stationary actors the fields hold values in the billions — so carrying all four would put
        // nonsense where a route is expected.
        Assert.True(Place(EncounterActorSpawn.Pending, out EncounterActorPlacement.Placed p, out _));

        Assert.Equal(RoamingMovement.WaypointCount(RoamingMovement.Pattern.BackAndForth),
            p.WaypointX.Count);
        Assert.Equal(2, p.WaypointX.Count);
    }

    [Fact]
    public void AStationaryActorCarriesNoRouteAtAll() {
        var still = new EnemySlot {
            MovementPattern = (ushort)RoamingMovement.Pattern.Stationary,
            AltSpawnX = new[] { 9, 9, 9, 9 }, AltSpawnY = new[] { 9, 9, 9, 9 },
        };
        Assert.True(EncounterActorPlacement.TryPlace(EncounterActorSpawn.Standing, false, still,
            Pose(0, 0, 0), TileX, TileY, 0, 0, out EncounterActorPlacement.Placed p, out _));

        Assert.Equal(RoamingMovement.Pattern.Stationary, p.Pattern);
        Assert.Empty(p.WaypointX);
    }

    [Fact]
    public void TheREBASEDoesNotMutateTheRecord() {
        // The original rebases a COPY. In place, the origin would be added again on the next chunk
        // and the route would drift a tile further away every time the party walked back.
        EnemySlot template = Template();
        EncounterActorPlacement.TryPlace(EncounterActorSpawn.Pending, false, template,
            Pose(0, 0, 0), TileX, TileY, 0, 0, out _, out _);
        EncounterActorPlacement.TryPlace(EncounterActorSpawn.Pending, false, template,
            Pose(0, 0, 0), TileX, TileY, 0, 0, out EncounterActorPlacement.Placed second, out _);

        Assert.Equal(1000, template.AltSpawnX[0]);
        Assert.Equal((TileX * (long)WorldPlacement.TileSize) + 1000, second.WaypointX[0]);
    }

    [Fact]
    public void TheChunkCapIsTheSaveBlocksOwnStride() {
        // Five records of seven slots. Applied per record instead of per chunk it would allow 35.
        Assert.Equal(EncounterObjectStates.EntriesPerRefPair,
            EncounterActorPlacement.MaxPlacedPerChunk);
        Assert.Equal(EncounterObjectStates.RecordsPerRefPair * EncounterObjectStates.SlotsPerRecord,
            EncounterActorPlacement.MaxPlacedPerChunk);
    }

    [Fact]
    public void TheSlotIndexOverloadCarriesItThrough() {
        Assert.True(EncounterActorPlacement.TryPlace(5, EncounterActorSpawn.Standing, false,
            Template(), Pose(1, 2, 3), TileX, TileY, 0, 0,
            out EncounterActorPlacement.Placed p, out _));

        Assert.Equal(5, p.RosterSlot);
        Assert.Equal((TileX * (long)WorldPlacement.TileSize) + 1, p.WorldX);
    }
}
