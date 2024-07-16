namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Loads a sound from the sound file and stores it in a list.
/// </summary>
public class LoadSound : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(LoadSound)}({SoundId});";
    }
}