namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageRotated180Scaled : DrawImageBase {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageRotated180Scaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}