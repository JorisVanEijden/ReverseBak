namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Billboard facing and the walk cycle for roaming encounter actors. The ping-pong gait and the
/// mirrored half-turn are what a port loses.
/// </summary>
public class EncounterActorPoseTests {
    private static List<int> Cycle(int steps) {
        var frames = new List<int>();
        var frame = 0;
        var advancing = false;

        for (var i = 0; i < steps; i++) {
            EncounterActorPose.Advance(ref frame, ref advancing);
            frames.Add(frame);
        }

        return frames;
    }

    [Fact]
    public void TheWalkCycleIsAPingPongNotALoop() {
        // 0,1,2,1,0,1,2,1... — the middle frame is passed through twice per cycle. A three-frame
        // loop snaps the leg back instead of swinging it.
        Assert.Equal(new[] { 1, 2, 1, 0, 1, 2, 1, 0 }, Cycle(8));
    }

    [Fact]
    public void TheCycleReversesAtBothEndsRatherThanWrapping() {
        var frame = 2;
        var advancing = true;

        EncounterActorPose.Advance(ref frame, ref advancing);

        Assert.Equal(1, frame);
        Assert.False(advancing);
    }

    [Fact]
    public void EightFacingsAreDrawnFromFiveColumnsPlusMirroring() {
        // A creature's sheet only ever holds half a turn; the far side is the near side flipped.
        var columns = new List<(int Column, bool Mirrored)>();

        for (var octant = 0; octant < EncounterActorPose.Octants; octant++) {
            int column = EncounterActorPose.SpriteColumn(EncounterActorPose.WalkingKind, octant, out bool mirrored);
            columns.Add((column, mirrored));
        }

        // *** OCTANT 6 WAS PINNED AT 0 HERE UNTIL 2026-08-26, AND IT IS 6. *** The original's
        // `case 6:` sets the mirror flag and does not reassign the column, and this test had
        // codified reading that empty arm as "falls to zero" — a passing test pinning the reading
        // rather than the game.
        Assert.Equal(
            new[] {
                (0, false), (3, false), (6, false), (9, false),
                (12, false), (9, true), (6, true), (3, true),
            },
            columns);
    }

    [Fact]
    public void TheFarHalfOfTheTurnMIRRORSTheNearHalf() {
        // The check that would have caught the wrong column without anyone re-reading the original:
        // the far octants are the near ones reflected about octant 4, so 5<->3, 6<->2 and 7<->1 must
        // share a column and differ only in the mirror flag. A stray value breaks exactly one pair.
        for (var octant = 5; octant < EncounterActorPose.Octants; octant++) {
            int mirrorOf = EncounterActorPose.Octants - octant;   // 5->3, 6->2, 7->1

            int far = EncounterActorPose.SpriteColumn(
                EncounterActorPose.WalkingKind, octant, out bool farMirrored);
            int near = EncounterActorPose.SpriteColumn(
                EncounterActorPose.WalkingKind, mirrorOf, out bool nearMirrored);

            Assert.Equal(near, far);
            Assert.True(farMirrored, $"octant {octant} is the far side and must be mirrored");
            Assert.False(nearMirrored, $"octant {mirrorOf} is the near side and must not be");
        }
    }

    [Fact]
    public void AStandingActorResolvesToAQuadrantWithItsOwnColumns() {
        // Not a walking actor with fewer frames: different stride, and the first column is 3 rather
        // than 0 — its sheet does not start at column zero.
        Assert.Equal(3, EncounterActorPose.SpriteColumn(EncounterActorPose.StandingKind, 0, out _));
        Assert.Equal(3, EncounterActorPose.SpriteColumn(EncounterActorPose.StandingKind, 1, out _));
        Assert.Equal(7, EncounterActorPose.SpriteColumn(EncounterActorPose.StandingKind, 2, out _));
        Assert.Equal(11, EncounterActorPose.SpriteColumn(EncounterActorPose.StandingKind, 4, out _));

        EncounterActorPose.SpriteColumn(EncounterActorPose.StandingKind, 7, out bool mirrored);
        Assert.True(mirrored);
    }

    [Fact]
    public void OnlyTheTwoKnownKindsAreDrawn() {
        Assert.True(EncounterActorPose.IsDrawn(EncounterActorPose.WalkingKind));
        Assert.True(EncounterActorPose.IsDrawn(EncounterActorPose.StandingKind));
        Assert.False(EncounterActorPose.IsDrawn(0));
        Assert.False(EncounterActorPose.IsDrawn(5));
    }

    [Fact]
    public void TheFacingIsTakenRelativeToTheActorsOwnHeading() {
        // Same camera angle, actor turned a quarter turn: the sprite shown moves by two octants.
        int straightOn = EncounterActorPose.Octant(0, 0);
        int turned = EncounterActorPose.Octant(0, EncounterActorPose.QuarterTurn);

        Assert.NotEqual(straightOn, turned);
        Assert.Equal(2, ((turned - straightOn) + EncounterActorPose.Octants) % EncounterActorPose.Octants);
    }

    [Fact]
    public void TheOctantIsAlwaysInRangeHoweverTheAnglesWrap() {
        // The arithmetic is 16-bit; widening it would let the complement stop wrapping.
        for (var angle = 0; angle < EncounterActorPose.FullTurn; angle += 977) {
            Assert.InRange(EncounterActorPose.Octant(angle, 0), 0, EncounterActorPose.Octants - 1);
            Assert.InRange(EncounterActorPose.Octant(0, angle), 0, EncounterActorPose.Octants - 1);
            Assert.InRange(EncounterActorPose.Octant(-angle, angle), 0, EncounterActorPose.Octants - 1);
        }
    }

    [Fact]
    public void EveryOctantIsReachableAsTheCameraGoesRound() {
        var seen = new HashSet<int>();

        for (var angle = 0; angle < EncounterActorPose.FullTurn; angle += 64) {
            seen.Add(EncounterActorPose.Octant(angle, 0));
        }

        Assert.Equal(EncounterActorPose.Octants, seen.Count);
    }

    [Fact]
    public void TheStateWordRoundTrips() {
        ushort packed = EncounterActorPose.PackState(EncounterActorPose.WalkingKind, 2, true);

        EncounterActorPose.UnpackState(packed, out int kind, out int frame, out bool advancing);

        Assert.Equal(EncounterActorPose.WalkingKind, kind);
        Assert.Equal(2, frame);
        Assert.True(advancing);
    }

    [Fact]
    public void AStoppedGaitIsDistinctFromAnAdvancingOneAtTheSameFrame() {
        Assert.NotEqual(
            EncounterActorPose.PackState(EncounterActorPose.WalkingKind, 1, true),
            EncounterActorPose.PackState(EncounterActorPose.WalkingKind, 1, false));
    }

    [Fact]
    public void AnOctantOutsideTheEightIsRefusedRatherThanIndexingPastTheTable() {
        Assert.Equal(0, EncounterActorPose.SpriteColumn(EncounterActorPose.WalkingKind, 8, out _));
        Assert.Equal(0, EncounterActorPose.SpriteColumn(EncounterActorPose.WalkingKind, -1, out _));
    }
}
