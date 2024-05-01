namespace GameData.Resources.Animation.Commands;

public class DrawImageCommand2 : AnimatorCommand {
    public int Arg6 { get; set; }

    public int Arg5 { get; set; }

    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"DrawImage2({X}, {Y}, {ImageNumber}, {ImageResource}, {Arg5}, {Arg6});";
    }
}