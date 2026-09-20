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

    /// <summary>
    /// <b>The walking column is the BASE of a three-frame group, and the gait indexes within it.</b>
    /// </summary>
    /// <remarks>
    /// <c>WalkingColumns</c> is { 0, 3, 6, 9, 12, 9, 6, 3 } — a stride of exactly
    /// <see cref="EncounterActorPose.WalkFrames"/>. `DirectionalSprite` draws
    /// <c>SpriteColumn(...) + frame</c> on that assumption, so if the stride and the frame count
    /// ever disagree the gait would index a NEIGHBOURING FACING'S art rather than the next frame of
    /// its own — a bug that looks like the creature flickering between directions as it walks.
    ///
    /// <para>Checked as a property of the table rather than by listing it, so an added facing or a
    /// re-derived column set has to satisfy it too.</para>
    /// </remarks>
    [Fact]
    public void EachWalkingFacingOwnsExactlyWalkFramesColumns() {
        var bases = new System.Collections.Generic.List<int>();
        for (var octant = 0; octant < EncounterActorPose.Octants; octant++) {
            bases.Add(EncounterActorPose.SpriteColumn(
                EncounterActorPose.WalkingKind, octant, out _));
        }

        foreach (int start in new System.Collections.Generic.HashSet<int>(bases)) {
            foreach (int frame in new[] { 0, EncounterActorPose.WalkFrames - 1 }) {
                int drawn = start + frame;
                bool collides = false;
                foreach (int other in bases) {
                    if (other != start && drawn >= other && drawn < other + EncounterActorPose.WalkFrames) {
                        collides = true;
                    }
                }
                Assert.False(collides,
                    $"column {start} frame {frame} lands inside another facing's group");
            }
        }
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
        Assert.Equal(3, EncounterActorPose.SpriteColumn(EncounterActorPose.DownedKind, 0, out _));
        Assert.Equal(3, EncounterActorPose.SpriteColumn(EncounterActorPose.DownedKind, 1, out _));
        Assert.Equal(7, EncounterActorPose.SpriteColumn(EncounterActorPose.DownedKind, 2, out _));
        Assert.Equal(11, EncounterActorPose.SpriteColumn(EncounterActorPose.DownedKind, 4, out _));

        EncounterActorPose.SpriteColumn(EncounterActorPose.DownedKind, 7, out bool mirrored);
        Assert.True(mirrored);
    }

    [Fact]
    public void OnlyTheTwoKnownKindsAreDrawn() {
        Assert.True(EncounterActorPose.IsDrawn(EncounterActorPose.WalkingKind));
        Assert.True(EncounterActorPose.IsDrawn(EncounterActorPose.DownedKind));
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

/// <summary>
/// A facing is the column AND the mirror — the column alone names two of them.
/// </summary>
/// <remarks>
/// TASK-595: combat sprites only ever faced up, down and right. Five sheets cover eight facings by
/// drawing three of them mirrored, so octants 7/1, 6/2 and 5/3 share a column number. The renderer
/// cached built columns under that number alone and compared it alone to decide whether anything had
/// changed, so turning from a right-facing octant to its left-facing twin was a no-op twice over:
/// nothing rebuilt, and the collision handed back whichever handedness had been built first.
///
/// <para>These pin the premise the renderer's cache key rests on. They are not a substitute for
/// looking at the screen, which is what a rendering defect ultimately needs.</para>
/// </remarks>
public class EncounterActorFacingIdentityTests {
    private static (int Column, bool Mirrored) Facing(int octant) {
        int column = EncounterActorPose.SpriteColumn(
            EncounterActorPose.WalkingKind, octant, out bool mirrored);
        return (column, mirrored);
    }

    [Theory]
    [InlineData(1, 7)]
    [InlineData(2, 6)]
    [InlineData(3, 5)]
    public void TheLeftFacingOctantSharesItsTwinsColumnAndDiffersOnlyInTheMirror(int right, int left) {
        Assert.Equal(Facing(right).Column, Facing(left).Column);
        Assert.False(Facing(right).Mirrored);
        Assert.True(Facing(left).Mirrored);
    }

    [Fact]
    public void TheColumnAloneDoesNotIdentifyAFacing_ButThePairDoes() {
        var all = Enumerable.Range(0, EncounterActorPose.Octants).Select(Facing).ToList();

        // Three collisions is the whole point: 8 facings over 5 sheets.
        Assert.Equal(5, all.Select(f => f.Column).Distinct().Count());
        Assert.Equal(8, all.Distinct().Count());
    }

    [Fact]
    public void TheForwardAndBackFacingsAreNeverMirrored() {
        // Octants 0 and 4 face the camera and away from it; a mirror there would be a no-op that
        // still cost a rebuild, and the original does not do it (SpriteColumn mirrors at >= 5).
        Assert.False(Facing(0).Mirrored);
        Assert.False(Facing(4).Mirrored);
    }
}
