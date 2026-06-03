namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

public class DrawImageFlippedHorizontallyScaled : DrawImageBase, IArea {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageFlippedHorizontallyScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}