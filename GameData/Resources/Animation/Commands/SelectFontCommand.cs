namespace GameData.Resources.Animation.Commands;

public class SelectFontCommand : AnimatorCommand {
    public int FontNumber { get; set; }

    public override string ToString() {
        return $"SelectFont({FontNumber});";
    }
}