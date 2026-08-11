namespace GameData.Resources.Animation.FrameCommands;

public class LoadSoundResource : FrameCommand {
    public string? Filename { get; set; }

    public override string ToString() {
        return $"{nameof(LoadSoundResource)}('{Filename}');";
    }
}