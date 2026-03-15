namespace GameData.Resources.Data;

public class SaveGameWorldEncounterItemData {
    public SaveGameWorldEncounterItemData(
        int xPosition,
        int yPosition,
        short zRotation,
        short flags
    ) {
        XPosition = xPosition;
        YPosition = yPosition;
        ZRotation = zRotation;
        Flags = flags;
    }

    public int XPosition { get; }
    public int YPosition { get; }
    public short ZRotation { get; }
    public short Flags { get; }
}
