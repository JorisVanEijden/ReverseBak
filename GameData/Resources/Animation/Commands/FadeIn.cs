namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Fade in the current buffer.
/// </summary>
public class FadeIn : FrameCommand {
    public int Start { get; set; }

    public int End { get; set; }

    /// Palette index of the color to fade from
    public int Color { get; set; }

    /// Delay ranging from 0 to 6, determines the speed of the fade
    public int Delay { get; set; }

    public override string ToString() {
        return $"{nameof(FadeIn)}({Start}, {End}, {Color}, {Delay});";
    }
}