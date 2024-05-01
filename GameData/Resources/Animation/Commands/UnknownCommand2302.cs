namespace GameData.Resources.Animation.Commands;

public class UnknownCommand2302 : AnimatorCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2302({Arg1}, {Arg2});";
    }
}