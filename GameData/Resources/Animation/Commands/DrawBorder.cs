namespace GameData.Resources.Animation.Commands;

public class DrawBorder : FrameCommand, IArea {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawBorder)}({X}, {Y}, {Width}, {Height});";
    }
}