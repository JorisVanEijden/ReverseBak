namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA536 : FrameCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommandA536)}({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6});";
    }
}