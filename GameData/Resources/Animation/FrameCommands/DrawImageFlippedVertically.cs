namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageFlippedVertically : DrawImageBase {
    public override string ToString() {
        return $"{nameof(DrawImageFlippedVertically)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}