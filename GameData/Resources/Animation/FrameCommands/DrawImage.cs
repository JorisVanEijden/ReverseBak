namespace GameData.Resources.Animation.FrameCommands;

public class DrawImage : DrawImageBase {
    public override string ToString() {
        return $"{nameof(DrawImage)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}