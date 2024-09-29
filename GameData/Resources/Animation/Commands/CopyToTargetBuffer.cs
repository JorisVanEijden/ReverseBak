namespace GameData.Resources.Animation.Commands;

/**
 * Fills the current buffer with the contents of the specified area.
 */
public class CopyToTargetBuffer : FrameCommand, IArea {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(CopyToTargetBuffer)}({X}, {Y}, {Width}, {Height});";
    }
}