namespace GameData.Resources.Animation.Commands;

/// <summary>
/// Sets the currently selected image slot (bitmapArray)
/// </summary>
public class SelectImageSlot : FrameCommand {
    public int SlotNumber { get; set; }

    public override string ToString() {
        return $"{nameof(SelectImageSlot)}({SlotNumber});";
    }
}