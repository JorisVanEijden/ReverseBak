namespace GameData.Resources.Animation.Commands;

public class ClearAreaCommand : AnimatorCommand {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"ClearArea({X}, {Y}, {Width}, {Height});";
    }
}