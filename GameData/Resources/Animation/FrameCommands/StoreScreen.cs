namespace GameData.Resources.Animation.FrameCommands;

/// <summary>
/// Copy buffer B to buffer C
/// </summary>
public class StoreScreen : FrameCommand {
    public override string ToString() {
        return $"{nameof(StoreScreen)}();";
    }
}