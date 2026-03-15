namespace GameData.Resources.Animation.FrameCommands;

/// <summary>
/// Set the specified frame as the next to execute.
/// </summary>
public class GotoFrame : FrameCommand {
    public int NextFrame { get; set; }

    public override string ToString() {
        return $"{nameof(GotoFrame)}({NextFrame});";
    }
}