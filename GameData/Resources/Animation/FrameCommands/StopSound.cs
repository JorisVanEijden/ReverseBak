namespace GameData.Resources.Animation.FrameCommands;

public class StopSound : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(StopSound)}({SoundId});";
    }
}