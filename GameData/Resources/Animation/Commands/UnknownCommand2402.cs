namespace GameData.Resources.Animation.Commands;

public class UnknownCommand2402 : FrameCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommand2402)}({Arg1}, {Arg2});";
    }
}