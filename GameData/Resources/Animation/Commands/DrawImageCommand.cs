namespace GameData.Resources.Animation.Commands;

public class DrawImageCommand : AnimatorCommand {
    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"DrawImage({X}, {Y}, {ImageNumber}, {ImageResource});";
    }
}