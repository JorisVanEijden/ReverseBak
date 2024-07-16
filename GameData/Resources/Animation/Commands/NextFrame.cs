namespace GameData.Resources.Animation.Commands;

internal class NextFrame : FrameCommand {
    public override string ToString() {
        return $"{nameof(NextFrame)}();";
    }
}