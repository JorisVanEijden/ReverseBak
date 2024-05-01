namespace GameData.Resources.Animation.Commands;

public class UnknownCommand2002 : AnimatorCommand {
    public int Arg2 { get; set; }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2002({Arg1}, {Arg2});";
    }
}