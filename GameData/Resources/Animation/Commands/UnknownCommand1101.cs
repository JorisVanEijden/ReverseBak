namespace GameData.Resources.Animation.Commands;

public class UnknownCommand1101 : FrameCommand {
    public int Arg1 { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommand1101)}({Arg1});";
    }
}