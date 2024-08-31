namespace GameData.Resources.Animation.Commands;

public class SetClipArea : FrameCommand, IArea {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(SetClipArea)}({X}, {Y}, {Width}, {Height});";
    }
}