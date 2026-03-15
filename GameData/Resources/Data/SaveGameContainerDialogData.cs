namespace GameData.Resources.Data;

public class SaveGameContainerDialogData {
    public SaveGameContainerDialogData(
        byte field0,
        byte field1,
        uint dialogId
    ) {
        Field0 = field0;
        Field1 = field1;
        DialogId = dialogId;
    }

    public byte Field0 { get; }
    public byte Field1 { get; }
    public uint DialogId { get; }
}
