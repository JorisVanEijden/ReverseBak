namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageFlippedHorizontallyScaled : DrawImageBase {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageFlippedHorizontallyScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}