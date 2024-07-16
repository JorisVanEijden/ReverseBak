namespace GameData.Resources.Animation.Commands;

/**
 * Fills the current buffer with the contents of the specified area.
 */
public class CopyToCurrentBuffer : FrameCommand {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(CopyToCurrentBuffer)}({X}, {Y}, {Width}, {Height});";
    }
}