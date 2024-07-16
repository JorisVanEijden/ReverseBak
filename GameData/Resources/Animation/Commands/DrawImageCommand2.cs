namespace GameData.Resources.Animation.Commands;

public class DrawImageCommand2 : FrameCommand {
    public int X { get; set; }
    public int Y { get; set; }
    public int ImageNumber { get; set; }
    public int ImageSlot { get; set; }
    public int Arg5 { get; set; }
    public int Arg6 { get; set; }

    public override string ToString() {
        return $"DrawImage2({X}, {Y}, {ImageNumber}, {ImageSlot}, {Arg5}, {Arg6});";
    }
}