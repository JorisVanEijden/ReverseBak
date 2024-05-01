namespace GameData.Resources.Animation.Commands;

public class SelectPaletteCommand : AnimatorCommand {
    public int PaletteNumber { get; set; }

    public override string ToString() {
        return $"SelectPalette({PaletteNumber});";
    }
}