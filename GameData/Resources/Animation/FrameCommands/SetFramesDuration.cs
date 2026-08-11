namespace GameData.Resources.Animation.FrameCommands;

/**
 * Specifies how long each frame is going to be shown.
 * 0 resets the time to the default (as yet unknown)
 */
public class SetFramesDuration : FrameCommand {
    public int Amount { get; set; }

    public override string ToString() {
        return $"{nameof(SetFramesDuration)}({Amount});";
    }
}