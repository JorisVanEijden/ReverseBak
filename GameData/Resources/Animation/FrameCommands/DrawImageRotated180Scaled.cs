namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

public class DrawImageRotated180Scaled : DrawImageBase, IArea {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageRotated180Scaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}