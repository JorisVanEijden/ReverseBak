namespace GameData.Resources.Animation.Commands;


// Copy area from buffer B to buffer C

public class StoreArea : FrameCommand, IArea {
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(StoreArea)}({X}, {Y}, {Width}, {Height});";
    }
}