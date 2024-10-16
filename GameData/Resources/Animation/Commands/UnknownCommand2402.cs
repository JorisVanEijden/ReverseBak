namespace GameData.Resources.Animation.Commands;

// This seems to do something with palette ranges.
public class UnknownCommand2402 : FrameCommand {
    public Ranges Range { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommand2402)}({Range}, {Arg2});";
    }
}

[Flags]
public enum Ranges : ushort {
    Range1 = 1,
    Range2 = 2,
    Range3 = 4,
}