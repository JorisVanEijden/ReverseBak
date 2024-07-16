namespace GameData.Resources.Animation.Commands;

public class SetRange1 : FrameCommand {
    public int Start { get; set; }

    public int End { get; set; }

    public override string ToString() {
        return $"{nameof(SetRange1)}({Start}, {End});";
    }
}