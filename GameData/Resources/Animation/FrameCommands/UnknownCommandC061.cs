namespace GameData.Resources.Animation.FrameCommands;

public class UnknownCommandC061 : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(UnknownCommandC061)}({SoundId});";
    }
}