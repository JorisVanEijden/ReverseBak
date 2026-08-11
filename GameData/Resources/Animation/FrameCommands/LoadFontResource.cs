namespace GameData.Resources.Animation.FrameCommands;

public class LoadFontResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadFontResource)}('{Filename}');";
    }
}