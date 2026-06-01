namespace GameData.Resources.Animation.FrameCommands;

// TTM 0xA034: screen transition over the rectangle (X, Y, Width, Height) using
// the "box in" style — a hollow rectangle that shrinks toward the centre,
// progressively copying the off-screen frame (buffer A) into the visible
// buffer. Verified in anim_screenTransitionEffect (IDA @ 0x53ab5).
public class ScreenTransitionBoxIn : FrameCommand {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(ScreenTransitionBoxIn)}({X}, {Y}, {Width}, {Height});";
    }
}
