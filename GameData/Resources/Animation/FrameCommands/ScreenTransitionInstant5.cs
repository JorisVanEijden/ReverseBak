namespace GameData.Resources.Animation.FrameCommands;

// TTM 0xA0B5: 5-argument screen-transition variant over (X, Y, Width, Height).
// Like 0xA014 it resolves to the "instant" (no visible wipe) style in
// anim_screenTransitionEffect (IDA @ 0x53ab5); Arg5 is read from the script but
// ignored by the engine.
public class ScreenTransitionInstant5 : FrameCommand {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    // Read from the script but ignored by the engine.
    public int Arg5 { get; set; }

    public override string ToString() {
        return $"{nameof(ScreenTransitionInstant5)}({X}, {Y}, {Width}, {Height}, {Arg5});";
    }
}
