namespace GameData.Resources.Animation.Commands;

public class SetColors : FrameCommand {
    public int ForegroundColor { get; set; }
    public int BackgroundColor { get; set; }

    public override string ToString() {
        return $"{nameof(SetColors)}({ForegroundColor}, {BackgroundColor});";
    }
}