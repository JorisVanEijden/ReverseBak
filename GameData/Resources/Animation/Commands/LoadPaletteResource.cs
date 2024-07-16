namespace GameData.Resources.Animation.Commands;

/**
 * Loads a palette resource from the specified file into the current palette.
 */
public class LoadPaletteResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadPaletteResource)}('{Filename}');";
    }
}