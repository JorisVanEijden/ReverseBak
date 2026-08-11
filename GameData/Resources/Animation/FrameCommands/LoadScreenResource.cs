namespace GameData.Resources.Animation.FrameCommands;

public class LoadScreenResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadScreenResource)}('{Filename}');";
    }
}