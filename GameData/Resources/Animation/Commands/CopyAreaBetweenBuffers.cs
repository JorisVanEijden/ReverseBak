namespace GameData.Resources.Animation.Commands;

public class CopyAreaBetweenBuffers : FrameCommand, IArea {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { set; get; }
    public int Height { get; set; }
    public int SourceBuffer { get; set; }
    public int DestinationBuffer { get; set; }

    public override string ToString() {
        return $"{nameof(CopyAreaBetweenBuffers)}({X}, {Y}, {Width}, {Height}, {SourceBuffer}, {DestinationBuffer});";
    }

    /*
     * Draw a rectangle with the specified dimensions at the specified location using the specified video buffer.
     */
}