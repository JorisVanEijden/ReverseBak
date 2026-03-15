namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageScaled : DrawImageBase {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}