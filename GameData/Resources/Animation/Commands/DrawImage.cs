namespace GameData.Resources.Animation.Commands;

public class DrawImage : FrameCommand {
    public int X { get; set; }
    public int Y { get; set; }
    public int ImageNumber { get; set; }
    public int ImageSlot { get; set; }

    public override string ToString() {
        return $"{nameof(DrawImage)}({X}, {Y}, {ImageNumber}, {ImageSlot});";
    }
}