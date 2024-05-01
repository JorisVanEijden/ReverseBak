namespace GameData.Resources.Animation.Commands;

public class UnknownCommand2012 : AnimatorCommand {
    public int Arg2 { get; set; }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2012({Arg1}, {Arg2});";
    }
}