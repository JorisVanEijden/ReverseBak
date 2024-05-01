namespace GameData.Resources.Animation.Commands;

public class UnknownCommandA524 : AnimatorCommand {
    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommandA524({X}, {Y}, {ImageNumber}, {ImageResource});";
    }
}