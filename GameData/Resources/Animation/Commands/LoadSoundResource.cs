namespace GameData.Resources.Animation.Commands;

public class LoadSoundResource : AnimatorCommand {
    public LoadSoundResource(string soundFilename) {
        Filename = soundFilename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadSoundResource('{Filename}');";
    }
}