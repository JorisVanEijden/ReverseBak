namespace GameData.Resources.Animation.Commands;

public class UnknownCommandB606 : AnimatorCommand {
    public int Arg6 { get; set; }

    public int Arg5 { get; set; }

    public int Height { get; set; }

    public int Width { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommandB606({X}, {Y}, {Width}, {Height}, {Arg5}, {Arg6});";
    }
}