namespace GameData.Resources.Animation.Commands;

public class LoadScreenResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadScreenResource)}('{Filename}');";
    }
}