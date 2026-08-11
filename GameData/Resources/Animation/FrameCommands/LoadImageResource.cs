namespace GameData.Resources.Animation.FrameCommands;

public class LoadImageResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadImageResource)}('{Filename}');";
    }
}