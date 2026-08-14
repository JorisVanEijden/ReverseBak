namespace GameData.Resources.Animation.FrameCommands;

// TTM opcode 0x2402. Starts VGA palette colour-cycling on the windows defined
// by the preceding SetRange1/2/3 commands. Range selects which window(s) to
// cycle.
//
// ONLY THE SIGN OF Step IS USED. The handler computes `si = p1 / abs(p1)` and
// passes that ±1 as the rotation amount, so a Step of 7 and a Step of 1 cycle
// identically. A Step of ZERO is forced to 1 first, so it means "forward", not
// "do not cycle".
//
// abs(Step) IS read — into g_nPaletteCycleSavedDuration — and then NOTHING in the
// engine ever reads that global back. So the magnitude is genuinely inert, which
// answers the question this comment used to leave open rather than merely failing
// to find a reader.
//
// A POSITIVE Step ROTATES TOWARD LOWER PALETTE INDICES: the entry one past the
// window's start moves to the start. A negative one is normalised by
// palette_cycle_add (target = count + target) rather than reversed, which comes
// out as one step the other way.
//
// The command REPLACES any cycles already running: the handler calls
// palette_cycle_add(-1, 0, 0) first, which resets the list.
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
