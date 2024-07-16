namespace GameData.Resources.Animation.Commands;

public class StopSound : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(StopSound)}({SoundId});";
    }
}