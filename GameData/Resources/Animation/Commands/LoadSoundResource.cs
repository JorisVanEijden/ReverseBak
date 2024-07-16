namespace GameData.Resources.Animation.Commands;

public class LoadSoundResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadSoundResource)}('{Filename}');";
    }
}