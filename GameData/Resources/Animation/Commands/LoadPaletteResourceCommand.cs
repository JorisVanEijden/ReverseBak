namespace GameData.Resources.Animation.Commands;

public class LoadPaletteResourceCommand : AnimatorCommand {
    public LoadPaletteResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadPaletteResource('{Filename}');";
    }
}