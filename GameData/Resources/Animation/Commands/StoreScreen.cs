namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Copy buffer B to buffer C
/// </summary>
public class StoreScreen : FrameCommand {
    public override string ToString() {
        return $"{nameof(StoreScreen)}();";
    }
}