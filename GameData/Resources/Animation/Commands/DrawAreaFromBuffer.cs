namespace GameData.Resources.Animation.Commands;

public class DrawAreaFromBuffer : FrameCommand {
    public int BufferNumber { get; set; }

    public override string ToString() {
        return $"{nameof(DrawAreaFromBuffer)}({BufferNumber});";
    }
}