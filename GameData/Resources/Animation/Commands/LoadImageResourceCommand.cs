namespace GameData.Resources.Animation.Commands;

public class LoadImageResourceCommand : AnimatorCommand {
    public LoadImageResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadImageResource('{Filename}');";
    }
}