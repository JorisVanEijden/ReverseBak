namespace GameData.Resources.Animation.Commands;

public class LoadScreenResourceCommand : AnimatorCommand {
    public LoadScreenResourceCommand(string screenFilename) {
        Filename = screenFilename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadScreenResource('{Filename}');";
    }
}