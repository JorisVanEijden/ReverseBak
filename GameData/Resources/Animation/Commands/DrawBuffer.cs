namespace GameData.Resources.Animation.Commands;

public class DrawBuffer : FrameCommand {
    public int BufferNumber { get; set; }

    public override string ToString() {
        return $"{nameof(DrawBuffer)}({BufferNumber});";
    }
}