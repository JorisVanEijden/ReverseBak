namespace GameData.Resources.Animation.Commands;

public class SelectFontSlot : FrameCommand {
    public int SlotNumber { get; set; }

    public override string ToString() {
        return $"{nameof(SelectFontSlot)}({SlotNumber});";
    }
}