namespace GameData.Resources.Animation.Commands;

public class UnknownCommand4004 : AnimatorCommand {
    public int Height { get; set; }

    public int Width { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommand4004({X}, {Y}, {Width}, {Height});";
    }
}