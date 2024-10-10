namespace GameData.Resources.Animation.Commands;

public class DrawImageScaled : FrameCommand {
    public int X { get; set; }
    public int Y { get; set; }
    public int ImageNumber { get; set; }
    public int ImageSlot { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}