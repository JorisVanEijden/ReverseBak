namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageFlippedVerticallyScaled : DrawImageBase {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageFlippedVerticallyScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}