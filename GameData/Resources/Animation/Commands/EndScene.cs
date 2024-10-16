namespace GameData.Resources.Animation.Commands;

/**
 * This sets global bool at 39DD:4ECC to true.
 * Which is a signal to end the scene and return to the cutscene player state machine.
 */
public class EndScene : FrameCommand {
    public override string ToString() {
        return $"{nameof(EndScene)}();";
    }
}