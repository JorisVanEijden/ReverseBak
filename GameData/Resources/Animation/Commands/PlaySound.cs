namespace GameData.Resources.Animation.Commands;

public class PlaySound : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(PlaySound)}({SoundId});";
    }
}