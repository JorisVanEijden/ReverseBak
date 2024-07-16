namespace GameData.Resources.Animation.Commands;

/**
 * Set a delay.
 */
public class SetDelay : FrameCommand {
    public int Amount { get; set; }

    public override string ToString() {
        return $"{nameof(SetDelay)}({Amount});";
    }
}