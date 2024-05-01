namespace GameData.Resources.Animation.Commands;

public class UnknownCommand1101 : AnimatorCommand {
    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand1101({Arg1});";
    }
}