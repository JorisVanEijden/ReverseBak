namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

// TTM 0xA014: screen transition over the rectangle (X, Y, Width, Height) using
// the "instant" style. Verified in anim_screenTransitionEffect (IDA @ 0x53ab5):
// the A014 case restores the draw buffer and returns without copying, so it
// produces no visible wipe — the new frame simply appears.
public class ScreenTransitionInstant : FrameCommand, IArea {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(ScreenTransitionInstant)}({X}, {Y}, {Width}, {Height});";
    }
}
