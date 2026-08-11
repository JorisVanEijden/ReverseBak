namespace GameData.Resources.Animation.FrameCommands;

using GameData.Resources.Animation;

// This draws images rotated at a free angle, and scaled.
public class DrawImageRotated : DrawImageBase, IArea {
    private int _angle;
    public int Width { get; set; }
    public int Height { get; set; }

    public int Angle {
        get => _angle;
        set {
            _angle = value;
            float ax = (short)(-(value >> 4) + 4096) / (4096f / 360f);
        }
    }

    public override string ToString() {
        return $"{nameof(DrawImageRotated)}({X}, {Y}, {ImageNumber}, {ImageSlot}, {Width}, {Height}, {Angle});";
    }
}