namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The overhead map — <c>sub_ovr180_11F</c> (0x6d49f) over REQ_MAP.DAT.
/// </summary>
public class LocalMapScreenTests {
    [Fact]
    public void TheArrowsStillMoveAndTurnTHEPARTY() {
        // The screen is the world seen from above, not a drawn map — so its arrows are the travel
        // arrows, on the travel HUD's own action ids. A port that treats them as panning a picture
        // stops the player walking.
        Assert.Equal(LocalMapScreen.MapAction.MoveForward, LocalMapScreen.ActionFor(0x48));
        Assert.Equal(LocalMapScreen.MapAction.MoveBackward, LocalMapScreen.ActionFor(0x50));
        Assert.Equal(LocalMapScreen.MapAction.TurnLeft, LocalMapScreen.ActionFor(0x4b));
        Assert.Equal(LocalMapScreen.MapAction.TurnRight, LocalMapScreen.ActionFor(0x4d));
    }

    [Fact]
    public void EntryTipsTheCameraStraightDownAndLeavesTheYawALONE() {
        // canassa names the entry function map_camera_snap_face_south; IDA 0x6dbf7 writes 0xC000
        // into camera.rotation3d.x — a pitch, not a heading. Believing the name would leave the map
        // pointing south instead of down, and would explain away the north-up option below.
        Assert.Equal(unchecked((short)0xC000), LocalMapScreen.TopDownPitch);
        Assert.Equal(-LocalMapScreen.AngleUnitsPerTurn / 4, LocalMapScreen.TopDownPitch);
        Assert.True(LocalMapScreen.YawIsUntouchedOnEntry);
    }

    [Fact]
    public void NorthUpMovesTheHeadingOutOfTheCameraAndIntoTheMarker() {
        Assert.Equal(0x2000, LocalMapScreen.MapRendersWithYaw(0x2000, northUp: false));
        Assert.Equal(0, LocalMapScreen.MapRendersWithYaw(0x2000, northUp: true));
    }

    [Fact]
    public void ONLYTheFiveStepZoomsClampAndTHATFollowsFromTheButtons() {
        // The one-step arms write back with no bounds test; they are safe only because their
        // BUTTONS go dead at the limits. The five-step arms have no button to switch off, so they
        // clamp in the arithmetic instead.
        Assert.False(LocalMapScreen.HasButton(LocalMapScreen.MapAction.ZoomDownFiveSteps));
        Assert.False(LocalMapScreen.HasButton(LocalMapScreen.MapAction.ZoomUpFiveSteps));
        Assert.True(LocalMapScreen.HasButton(LocalMapScreen.MapAction.ZoomDownOneStep));
        Assert.True(LocalMapScreen.HasButton(LocalMapScreen.MapAction.ZoomUpOneStep));

        Assert.False(LocalMapScreen.ClampsItsOwnZoom(LocalMapScreen.MapAction.ZoomDownOneStep));
        Assert.False(LocalMapScreen.ClampsItsOwnZoom(LocalMapScreen.MapAction.ZoomUpOneStep));
        Assert.True(LocalMapScreen.ClampsItsOwnZoom(LocalMapScreen.MapAction.ZoomDownFiveSteps));
        Assert.True(LocalMapScreen.ClampsItsOwnZoom(LocalMapScreen.MapAction.ZoomUpFiveSteps));
    }

    [Fact]
    public void AnUnclampedStepReallyDoesLeaveTheRange() {
        // Pinned rather than quietly corrected: this is what the arithmetic does, and the guard
        // lives in the button's enable gate instead.
        Assert.Equal(-10,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.ZoomDownOneStep,
                cameraZ: 0, step: 10, minimum: 0, maximum: 100));
    }

    [Fact]
    public void TheFiveStepJumpStopsAtTheLimitRatherThanRefusing() {
        Assert.Equal(0,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.ZoomDownFiveSteps,
                cameraZ: 20, step: 10, minimum: 0, maximum: 100));
        Assert.Equal(100,
            LocalMapScreen.CameraZAfter(LocalMapScreen.MapAction.ZoomUpFiveSteps,
                cameraZ: 80, step: 10, minimum: 0, maximum: 100));
    }

    [Fact]
    public void TheButtonsGoDeadWhenAWHOLEStepNoLongerFits() {
        // The gate asks whether one full step still fits, not whether any movement is possible —
        // so the zoom stops a step short of the limit rather than creeping up to it.
        Assert.True(LocalMapScreen.CanZoomDown(cameraZ: 20, step: 10, minimum: 0));
        Assert.True(LocalMapScreen.CanZoomDown(cameraZ: 10, step: 10, minimum: 0));
        Assert.False(LocalMapScreen.CanZoomDown(cameraZ: 5, step: 10, minimum: 0));

        Assert.True(LocalMapScreen.CanZoomUp(cameraZ: 90, step: 10, maximum: 100));
        Assert.False(LocalMapScreen.CanZoomUp(cameraZ: 95, step: 10, maximum: 100));
    }

    [Fact]
    public void TheHelpTextIsTheTravelHUDsExtendedNotASecondSet() {
        // 223-227 and 229 are the records the travel HUD's own buttons show; only the four
        // map-specific ones are new. Writing fresh text for the shared buttons would say something
        // different on two screens that carry the same button.
        Assert.Equal(223, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.MoveForward));
        Assert.Equal(227, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ToggleFollowRoad));
        Assert.Equal(229, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.Encamp));
        Assert.Equal(235, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ShowFullMap));
        Assert.Equal(236, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.Close));
    }

    [Fact]
    public void TheZoomPairsShareTheirLine() {
        Assert.Equal(233, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ZoomDownOneStep));
        Assert.Equal(233, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ZoomDownFiveSteps));
        Assert.Equal(234, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ZoomUpOneStep));
        Assert.Equal(234, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ZoomUpFiveSteps));
    }

    [Fact]
    public void TheKeyboardOnlyControlsHaveNoHelpBecauseTheyHaveNoButton() {
        Assert.False(LocalMapScreen.HasButton(LocalMapScreen.MapAction.ToggleNonRotating));
        Assert.Equal(0, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.ToggleNonRotating));
    }

    [Fact]
    public void BothTheMapButtonAndEscapeClose() {
        // 0x32 is the same id the travel HUD opens the map with, so the button is a toggle across
        // the two screens.
        Assert.Equal(LocalMapScreen.MapAction.Close, LocalMapScreen.ActionFor(0x32));
        Assert.Equal(LocalMapScreen.MapAction.Close, LocalMapScreen.ActionFor(0x01));
    }

    [Fact]
    public void TheMapViewIsNotTheTravelRenderPointedDown() {
        // sub_seg021_231 fills the viewport with a pen and draws the depth-sorted items over it —
        // no sky, no horizon strip, no ground band. Verified the hard way: aiming the travel
        // renderer downwards shows the horizon backdrop and a fog-flat ground, not a map.
        Assert.True(LocalMapScreen.HasItsOwnRenderMode);
    }

    [Fact]
    public void UndergroundTheOverheadMapIsTheAutomapNotTheWorld() {
        // drawMap runs renderDungeonAutomap for an underground zone and never reaches the 3D pass,
        // so underground the screen shows only what has been explored.
        Assert.True(LocalMapScreen.DrawsDungeonAutomap(isUnderground: true));
        Assert.False(LocalMapScreen.DrawsDungeonAutomap(isUnderground: false));
    }

    [Fact]
    public void AnUnknownActionDoesNothingAndSaysNothing() {
        Assert.Equal(LocalMapScreen.MapAction.None, LocalMapScreen.ActionFor(0x1234));
        Assert.Equal(0, LocalMapScreen.ZoomStepsFor(LocalMapScreen.MapAction.None));
        Assert.Equal(0, LocalMapScreen.DescribeDialogFor(LocalMapScreen.MapAction.None));
        Assert.False(LocalMapScreen.ClampsItsOwnZoom(LocalMapScreen.MapAction.None));
    }

    [Fact]
    public void MovingAndTurningChangeNoCameraHeight() {
        foreach (LocalMapScreen.MapAction a in new[] {
            LocalMapScreen.MapAction.MoveForward, LocalMapScreen.MapAction.MoveBackward,
            LocalMapScreen.MapAction.TurnLeft, LocalMapScreen.MapAction.TurnRight,
            LocalMapScreen.MapAction.ToggleFollowRoad, LocalMapScreen.MapAction.Close }) {
            Assert.Equal(0, LocalMapScreen.ZoomStepsFor(a));
        }
    }

    [Fact]
    public void TheZoomSurvivesClosingTheScreen() {
        Assert.True(LocalMapScreen.ZoomIsRemembered);
    }
}
