namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The in-zone map — <c>map_main_loop</c> over REQ_MAP.DAT.
/// </summary>
public class LocalMapScreenTests {
    [Fact]
    public void TheArrowsStillMoveAndTurnTHEPARTY() {
        // The screen is the world seen from above, not a drawn map — so its arrows are the travel
        // arrows. A port that treats them as panning a picture stops the player walking.
        Assert.Equal(LocalMapScreen.MapAction.MoveForward, LocalMapScreen.ActionFor(0x48));
        Assert.Equal(LocalMapScreen.MapAction.MoveBackward, LocalMapScreen.ActionFor(0x50));
        Assert.Equal(LocalMapScreen.MapAction.TurnLeft, LocalMapScreen.ActionFor(0x4b));
        Assert.Equal(LocalMapScreen.MapAction.TurnRight, LocalMapScreen.ActionFor(0x4d));
    }

    [Fact]
    public void ONLYTheFiveStepMovesClamp() {
        // The single-step arms write back with no bounds test. They are safe only because the
        // BUTTONS go dead at the limits, so a port that keeps the arithmetic and leaves the buttons
        // live walks the camera out of range.
        Assert.False(LocalMapScreen.ClampsItsOwnMove(LocalMapScreen.MapAction.LowerOneStep));
        Assert.False(LocalMapScreen.ClampsItsOwnMove(LocalMapScreen.MapAction.RaiseOneStep));
        Assert.True(LocalMapScreen.ClampsItsOwnMove(LocalMapScreen.MapAction.LowerFiveSteps));
        Assert.True(LocalMapScreen.ClampsItsOwnMove(LocalMapScreen.MapAction.RaiseFiveSteps));
    }

    [Fact]
    public void AnUnclampedStepReallyDoesLeaveTheRange() {
        // Pinned rather than quietly corrected: this is what the arithmetic does, and the guard
        // lives in the enable gate instead.
        Assert.Equal(-10,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.LowerOneStep,
                cameraZ: 0, step: 10, minimum: 0, maximum: 100));
    }

    [Fact]
    public void TheFiveStepJumpStopsAtTheLimitRatherThanRefusing() {
        Assert.Equal(0,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.LowerFiveSteps,
                cameraZ: 20, step: 10, minimum: 0, maximum: 100));
        Assert.Equal(100,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.RaiseFiveSteps,
                cameraZ: 80, step: 10, minimum: 0, maximum: 100));
    }

    [Fact]
    public void TheButtonsGoDeadWhenAWHOLEStepNoLongerFits() {
        // The gate asks whether one full step still fits, not whether any movement is possible —
        // so the camera stops a step short of the limit rather than creeping up to it.
        Assert.True(LocalMapScreen.CanLower(cameraZ: 20, step: 10, minimum: 0));
        Assert.True(LocalMapScreen.CanLower(cameraZ: 10, step: 10, minimum: 0));
        Assert.False(LocalMapScreen.CanLower(cameraZ: 5, step: 10, minimum: 0));

        Assert.True(LocalMapScreen.CanRaise(cameraZ: 90, step: 10, maximum: 100));
        Assert.False(LocalMapScreen.CanRaise(cameraZ: 95, step: 10, maximum: 100));
    }

    [Fact]
    public void SIXDescribeLinesForEightControlsBecauseTheZoomPairsShare() {
        // The wording is about the direction rather than the size, which is why there is no
        // "you cannot go much further" variant.
        Assert.Equal(LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.LowerOneStep),
            LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.LowerFiveSteps));
        Assert.Equal(LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.RaiseOneStep),
            LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.RaiseFiveSteps));

        var distinct = new System.Collections.Generic.HashSet<int>();
        foreach (LocalMapScreen.MapAction a in System.Enum.GetValues(typeof(LocalMapScreen.MapAction))) {
            int d = LocalMapScreen.DescribeDialogFor(a);
            if (d != 0) {
                distinct.Add(d);
            }
        }
        Assert.Equal(6, distinct.Count);
    }

    [Fact]
    public void AnUnknownActionDoesNothingAndSaysNothing() {
        Assert.Equal(LocalMapScreen.MapAction.None, LocalMapScreen.ActionFor(0x1234));
        Assert.Equal(0, LocalMapScreen.StepsFor(LocalMapScreen.MapAction.None));
        Assert.Equal(0, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.None));
    }

    [Fact]
    public void MovingAndTurningChangeNoCameraHeight() {
        foreach (LocalMapScreen.MapAction a in new[] {
            LocalMapScreen.MapAction.MoveForward, LocalMapScreen.MapAction.MoveBackward,
            LocalMapScreen.MapAction.TurnLeft, LocalMapScreen.MapAction.TurnRight }) {
            Assert.Equal(0, LocalMapScreen.StepsFor(a));
        }
    }
}
