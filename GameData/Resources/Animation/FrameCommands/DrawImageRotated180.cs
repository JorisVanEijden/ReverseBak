namespace GameData.Resources.Animation.FrameCommands;

public class DrawImageRotated180 : DrawImageBase {
    public override string ToString() {
        return $"{nameof(DrawImageRotated180)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}