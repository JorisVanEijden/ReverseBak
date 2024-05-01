namespace GameData.Resources.Animation.Commands;

public class TagCommand : AnimatorCommand {
    public string? TagName { get; set; }

    public int TagNumber { get; set; }

    public override string ToString() {
        return $"Tag({TagNumber});";
    }
}