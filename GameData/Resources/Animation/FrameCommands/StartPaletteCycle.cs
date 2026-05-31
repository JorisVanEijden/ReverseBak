namespace GameData.Resources.Animation.FrameCommands;

// TTM opcode 0x2402. Starts VGA palette colour-cycling on the windows defined
// by the preceding SetRange1/2/3 commands. Range selects which window(s) to
// cycle; Step's sign selects the cycle direction (the original advances one
// palette entry per timer tick). See docs for the unresolved meaning of
// abs(Step) — no static reader was found in the DOS binary.
public class StartPaletteCycle : FrameCommand {
    public Ranges Range { get; set; }

    public int Step { get; set; }

    public override string ToString() {
        return $"{nameof(StartPaletteCycle)}({Range}, {Step});";
    }
}

[Flags]
public enum Ranges : ushort {
    Range1 = 1,
    Range2 = 2,
    Range3 = 4,
}
