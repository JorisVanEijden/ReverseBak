namespace GameData.Resources.Animation.FrameCommands;

// TTM 0xA094: screen transition over the rectangle (X, Y, Width, Height) using
// the "box out" style — a box that grows outward from the centre, progressively
// copying the off-screen frame (buffer A) into the visible buffer. Verified in
// anim_screenTransitionEffect (IDA @ 0x53ab5).
public class ScreenTransitionBoxOut : FrameCommand {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(ScreenTransitionBoxOut)}({X}, {Y}, {Width}, {Height});";
    }
}
