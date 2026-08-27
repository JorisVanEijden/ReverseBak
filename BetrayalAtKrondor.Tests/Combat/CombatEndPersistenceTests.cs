namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// How a fight writes its survivors back onto the world (TASK-239).
/// </summary>
/// <remarks>
/// Both rules here come from <c>combat_actor_deploy_encounter</c> (CACTOR.C:343) — which despite its
/// name runs at fight END, from <c>combat_arena_finalize_round</c>. The position half is already
/// <see cref="CombatArenaPlacement.CellOffset"/>; these are the two rules that had no other home.
/// </remarks>
public class CombatEndPersistenceTests {
    // The original's own spelling, kept so the collapsed form can be checked against it rather than
    // against my reading of it.
    private static ushort AsTheOriginalWritesIt(ushort yaw) {
        var snapped = (ushort)(yaw & 0xE000);
        if ((yaw & 0x1000) != 0) {
            snapped = unchecked((ushort)(snapped + 0x2000));
        }
        return snapped;
    }

    [Fact]
    public void THESNAPRoundsToNearest_ItDoesNotTruncate() {
        // *** The rule reads as a truncation in the source. *** `yaw & 0xE000` alone is truncation,
        // and the `+= 45°` correction below it is what makes the pair a rounding — so a port that
        // takes the mask and misses the correction is wrong for half of every octant.
        Assert.Equal(0x2000, CombatEndPersistence.SnapToOctant(0x1800));   // 33.75 deg -> 45
        Assert.Equal(0x0000, CombatEndPersistence.SnapToOctant(0x0FFF));   // just under 22.5 -> 0
        Assert.Equal(0x2000, CombatEndPersistence.SnapToOctant(0x1000));   // exactly 22.5 -> up
    }

    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x0FFF)]
    [InlineData(0x1000)]
    [InlineData(0x1800)]
    [InlineData(0x4321)]
    [InlineData(0xE000)]
    [InlineData(0xF000)]
    [InlineData(0xFFFF)]
    public void THECOLLAPSEDFormMatchesTheOriginalsTwoSteps(int yaw) =>
        // The two-step form is what the source says; bias-then-mask is what is implemented. They
        // have to agree everywhere, including across the wrap — this is the check that the
        // simplification is a simplification and not a change.
        Assert.Equal(AsTheOriginalWritesIt((ushort)yaw),
            CombatEndPersistence.SnapToOctant((ushort)yaw));

    [Fact]
    public void ThESNAPWrapsRatherThanLeavingTheHeadingSpace() {
        // A yaw in the last sixteenth rounds up past a full turn. Written with a widening add it
        // lands at 0x10000, which is not a heading.
        Assert.Equal(0x0000, CombatEndPersistence.SnapToOctant(0xF000));
        Assert.Equal(0x0000, CombatEndPersistence.SnapToOctant(0xFFFF));
    }

    [Fact]
    public void THEFACINGIsComposedFromTheCAMERANotTheActor() {
        // animationFacing is an eighth-turn INDEX (0..7), not a heading. With the camera at 0 the
        // whole composition is the index plus the half turn.
        Assert.Equal(CombatEndPersistence.HalfTurn, CombatEndPersistence.FacingFor(0, 0));
        Assert.Equal((ushort)(CombatEndPersistence.Octant + CombatEndPersistence.HalfTurn),
            CombatEndPersistence.FacingFor(1, 0));
    }

    [Fact]
    public void TURNINGTheCameraMovesEveryPersistedFacingWithIt() {
        // *** The reason this cannot be the actor's own yaw. *** The arena is laid out relative to
        // the party's line of sight, so the same actor in the same grid tile persists differently
        // depending on which way the party was looking when the fight ended.
        ushort facingNorth = CombatEndPersistence.FacingFor(2, 0x0000);
        ushort facingEast = CombatEndPersistence.FacingFor(2, 0x4000);

        Assert.NotEqual(facingNorth, facingEast);
        Assert.Equal((ushort)(facingNorth + 0x4000), facingEast);
    }

    [Fact]
    public void THEFACINGWrapsInSixteenBits() {
        // Index 7 plus a half turn plus a snapped camera overflows a full turn for most yaws; the
        // result must stay a heading.
        // 0xE000 + 0xC000 + 0x8000 = 0x22000, which is two full turns past a heading. The expected
        // value has to be written `unchecked` for the same reason the implementation is: the
        // compiler rejects the constant outright, which is a fair warning about the arithmetic.
        ushort facing = CombatEndPersistence.FacingFor(7, 0xC000);
        Assert.Equal(unchecked((ushort)((7 * 0x2000) + 0xC000 + 0x8000)), facing);
        Assert.Equal((ushort)0x2000, facing);
    }

    [Fact]
    public void THETILEWalkCarriesIntoTheNextRow() {
        Assert.Equal((1, 0), CombatEndPersistence.NextTile(0, 0));
        Assert.Equal((0, 1), CombatEndPersistence.NextTile(CombatGrid.Width - 1, 0));
    }

    [Fact]
    public void THEWALKWrapsBackToTheTopRatherThanRunningOff() {
        // The last tile of the last row goes to (0,0). A port that let y run past the grid would
        // index off the end for an actor that finished in the far corner.
        Assert.Equal((0, 0),
            CombatEndPersistence.NextTile(CombatGrid.Width - 1, CombatGrid.Height - 1));
    }

    [Fact]
    public void AFREETileIsKept_WhichIsTheOrdinaryCase() =>
        Assert.Equal((3, 4), CombatEndPersistence.FreeTileFrom(3, 4, (x, y) => false));

    [Fact]
    public void ATAKENTileWalksToTheNextFreeOne() {
        // (3,4) and (4,4) taken; (5,4) is where it lands.
        (int X, int Y) at = CombatEndPersistence.FreeTileFrom(3, 4,
            (x, y) => (x == 3 && y == 4) || (x == 4 && y == 4));

        Assert.Equal((5, 4), at);
    }

    [Fact]
    public void ANALLOCCUPIEDGridDoesNotHang() {
        // *** The one place this diverges from the original, deliberately. *** It loops until it
        // finds a gap, relying on there being one. A predicate that says otherwise is a caller bug,
        // and hanging the fight's teardown is a worse answer than returning where the actor already
        // stood.
        Assert.Equal((2, 2), CombatEndPersistence.FreeTileFrom(2, 2, (x, y) => true));
    }

    [Fact]
    public void ANullPredicateLeavesTheActorWhereItIs() =>
        Assert.Equal((6, 6), CombatEndPersistence.FreeTileFrom(6, 6, null));
}
