namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageFlippedHorizontally : DrawImageBase {
    public override string ToString() {
        return $"{nameof(DrawImageFlippedHorizontally)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}