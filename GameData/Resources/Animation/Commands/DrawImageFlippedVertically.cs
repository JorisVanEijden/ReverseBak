namespace GameData.Resources.Animation.Commands;

public class DrawImageFlippedVertically : FrameCommand {
    public int X { get; set; }
    public int Y { get; set; }
    public int ImageNumber { get; set; }
    public int ImageSlot { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageFlippedVertically)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}