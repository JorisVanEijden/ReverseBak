namespace BetrayalAtKrondor.Tests.Location;

using GameData.Resources.Location;
using Xunit;

/// <summary>
/// The teleport dispatcher (<c>ProcessTeleportation</c> @0x4ebe7). Every teleport in the game — the
/// temple rift map, dialog teleport actions, ladders, tunnels — funnels through these rules.
/// </summary>
public class ZoneTransitionTests {
    private static Location At(int zone, int x = 5, int y = 5, int rotation = 0) =>
        new Location { ZoneNumber = zone, X = x, Y = y, ZRotation = rotation };

    // ---- the two sentinels ------------------------------------------------------------------

    [Fact]
    public void NothingQueuedIsZoneZero() =>
        Assert.Equal(ZoneTransitionKind.None, ZoneTransition.KindOf(At(0), 1, 5, 5, 0));

    [Fact]
    public void NoDestinationAtAllIsAlsoNothing() =>
        Assert.Equal(ZoneTransitionKind.None, ZoneTransition.KindOf(null, 1, 5, 5, 0));

    [Fact]
    public void TheSceneOnlySentinelIsStoredAsAByte() {
        // The shipped rows read 255; the original compares the same byte against -1.
        Assert.Equal(-1, ZoneTransition.NormalizeZone(255));
        Assert.Equal(ZoneTransitionKind.SceneOnly, ZoneTransition.KindOf(At(255), 1, 5, 5, 0));
        Assert.Equal(ZoneTransitionKind.SceneOnly, ZoneTransition.KindOf(At(-1), 1, 5, 5, 0));
    }

    [Fact]
    public void ARealZoneIsNotASentinel() {
        Assert.Equal(7, ZoneTransition.NormalizeZone(7));
        Assert.NotEqual(ZoneTransition.NoTransitionZone, ZoneTransition.NormalizeZone(7));
    }

    // ---- the three transitions ----------------------------------------------------------------

    [Fact]
    public void ADifferentZoneIsAFullChange() =>
        Assert.Equal(ZoneTransitionKind.ChangeZone,
            ZoneTransition.KindOf(At(9, x: 2, y: 3), currentZone: 1, currentX: 5, currentY: 5, currentRotation: 0));

    [Fact]
    public void TheSameZoneIsJustARepositioning() =>
        Assert.Equal(ZoneTransitionKind.Reposition,
            ZoneTransition.KindOf(At(1, x: 2, y: 3), currentZone: 1, currentX: 5, currentY: 5, currentRotation: 0));

    [Fact]
    public void AQueuedSceneRunsWheneverThereIsOne() {
        Assert.True(ZoneTransition.RunsAScene(70));
        Assert.False(ZoneTransition.RunsAScene(0));
    }

    // ---- the inverted y comparison -------------------------------------------------------------
    //
    // Reproduced deliberately, bug and all — see SkipsTheMove's remarks. These tests exist so the
    // quirk cannot be "tidied away" without a red suite and a decision.

    [Fact]
    public void SameZoneSameXDifferentYAndSameFacing_IsSilentlyDropped() =>
        // What the original does. Intended: this should have moved the party.
        Assert.Equal(ZoneTransitionKind.None,
            ZoneTransition.KindOf(At(1, x: 5, y: 9, rotation: 0),
                currentZone: 1, currentX: 5, currentY: 5, currentRotation: 0));

    [Fact]
    public void TeleportingOntoTheTileYouAlreadyStandOn_RelocatesAnyway() =>
        // The mirror of the same typo: the case the skip was meant to catch falls through instead.
        Assert.Equal(ZoneTransitionKind.Reposition,
            ZoneTransition.KindOf(At(1, x: 5, y: 5, rotation: 0),
                currentZone: 1, currentX: 5, currentY: 5, currentRotation: 0));

    [Fact]
    public void ChangingFacingAloneStillMoves() =>
        // Different y AND a turn: the skip needs the facing to match, so this one survives.
        Assert.Equal(ZoneTransitionKind.Reposition,
            ZoneTransition.KindOf(At(1, x: 5, y: 9, rotation: 512),
                currentZone: 1, currentX: 5, currentY: 5, currentRotation: 0));

    [Fact]
    public void TheIntendedTestDisagreesWithTheShippedOneOnExactlyTwoCases() {
        Location droppedByTheBug = At(1, x: 5, y: 9, rotation: 0);
        Location theRealNoOp = At(1, x: 5, y: 5, rotation: 0);

        Assert.True(ZoneTransition.SkipsTheMove(droppedByTheBug, 1, 5, 5, 0));
        Assert.False(ZoneTransition.SkipsTheMoveAsIntended(droppedByTheBug, 1, 5, 5, 0));

        Assert.False(ZoneTransition.SkipsTheMove(theRealNoOp, 1, 5, 5, 0));
        Assert.True(ZoneTransition.SkipsTheMoveAsIntended(theRealNoOp, 1, 5, 5, 0));
    }

    [Fact]
    public void ADifferentZoneIsNeverSkipped() =>
        // The zone comparison is the one branch that is not inverted, so a cross-zone move always
        // happens whatever the coordinates say.
        Assert.False(ZoneTransition.SkipsTheMove(At(9, x: 5, y: 9, rotation: 0), 1, 5, 5, 0));

    // ---- arrival --------------------------------------------------------------------------------

    [Fact]
    public void ArrivalLevelsTheCameraUnlessSomethingElseOwnsIt() {
        Assert.True(ZoneTransition.ResetsCameraHeightAndPitch(cameraOverrideActive: false));
        Assert.False(ZoneTransition.ResetsCameraHeightAndPitch(cameraOverrideActive: true));
    }
}
