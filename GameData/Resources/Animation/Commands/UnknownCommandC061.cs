namespace GameData.Resources.Animation.Commands;

public class UnknownCommandC061 : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommandC061)}({SoundId});";
    }
}