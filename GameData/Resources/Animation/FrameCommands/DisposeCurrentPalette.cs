namespace GameData.Resources.Animation.FrameCommands;

/**
 * Disposes the current palette.
  */
public class DisposeCurrentPalette : FrameCommand {
    public override string ToString() {
        return $"{nameof(DisposeCurrentPalette)}();";
    }
}