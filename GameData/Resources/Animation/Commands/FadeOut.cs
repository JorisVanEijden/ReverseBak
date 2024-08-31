namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Fade out the current buffer.
/// </summary>
public class FadeOut : FrameCommand {
    public int End { get; set; }

    public int Start { get; set; }

    /// Palette index of the color to fade to
    public int Color { get; set; }

    /// Speed ranging from 0 to 6, determines the speed of the fade
    public int Speed { get; set; }

    public override string ToString() {
        return $"{nameof(FadeOut)}({End}, {Start}, {Color}, {Speed});";
    }
}