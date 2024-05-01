namespace GameData.Resources.Animation.Commands;

public class LoadFontResourceCommand : AnimatorCommand {
    public LoadFontResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadFontResource('{Filename}');";
    }
}