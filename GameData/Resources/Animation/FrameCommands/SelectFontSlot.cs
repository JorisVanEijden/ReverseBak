namespace GameData.Resources.Animation.FrameCommands;

public class SelectFontSlot : FrameCommand {
    public int SlotNumber { get; set; }

    public override string ToString() {
        return $"{nameof(SelectFontSlot)}({SlotNumber});";
    }
}