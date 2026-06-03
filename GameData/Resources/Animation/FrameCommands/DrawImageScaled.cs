namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

public class DrawImageScaled : DrawImageBase, IArea {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImageScaled)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height});";
    }
}