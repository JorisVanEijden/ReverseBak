namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Makes the specified buffer the active buffer.
/// </summary>
public class SetTargetBuffer : FrameCommand {
    public int BufferNumber { get; set; }

    public override string ToString() {
        return $"{nameof(SetTargetBuffer)}({BufferNumber});";
    }
}