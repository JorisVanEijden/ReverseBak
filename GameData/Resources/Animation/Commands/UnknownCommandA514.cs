namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA514 : FrameCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommandA514)}({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}