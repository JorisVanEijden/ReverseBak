namespace GameData.Resources.Animation.Commands;

public class SelectImageCommand : AnimatorCommand {
    public int ImageNumber { get; set; }

    public override string ToString() {
        return $"SelectImage({ImageNumber});";
    }
}