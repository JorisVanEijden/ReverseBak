namespace GameData.Resources.Animation.Commands;

/**
 * Makes the specified palette slot the active palette slot.
 */
public class SelectPaletteSlot : FrameCommand {
    public int SlotNumber { get; set; }

    public override string ToString() {
        return $"{nameof(SelectPaletteSlot)}({SlotNumber});";
    }
}