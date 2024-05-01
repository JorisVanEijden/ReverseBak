namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA034 : AnimatorCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA034({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}