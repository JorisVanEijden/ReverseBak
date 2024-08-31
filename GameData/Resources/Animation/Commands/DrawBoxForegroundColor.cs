namespace GameData.Resources.Animation.Commands;

public class DrawBoxForegroundColor : FrameCommand, IArea {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawBoxForegroundColor)}({X}, {Y}, {Width}, {Height});";
    }
}