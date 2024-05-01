namespace GameData.Resources.Animation.Commands;

public class FadeInCommand : AnimatorCommand {
    public int Delay { get; set; }

    public int Step { get; set; }

    public int End { get; set; }

    public int Start { get; set; }

    public override string ToString() {
        return $"FadeIn({Start}, {End}, {Step}, {Delay});";
    }
}