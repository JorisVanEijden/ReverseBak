namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA5A7 : AnimatorCommand {
    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public int Arg7 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA5A7({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6}, {Arg7});";
    }
}