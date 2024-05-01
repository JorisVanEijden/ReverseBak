namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA601 : AnimatorCommand {
    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA601({Arg1});";
    }
}