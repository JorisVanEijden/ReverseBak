namespace GameData.Resources.Animation.FrameCommands;

public class DrawAreaFromBuffer : FrameCommand {
    public int BufferNumber { get; set; }

    public override string ToString() {
        return $"{nameof(DrawAreaFromBuffer)}({BufferNumber});";
    }
}