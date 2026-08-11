namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

public class DrawImageFlippedVerticallyScaled : DrawImageBase, IArea {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageFlippedVerticallyScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}