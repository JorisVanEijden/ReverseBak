namespace GameData.Resources.Data;

public class SaveGameZoneDataData {
    public SaveGameZoneDataData(
        short zoneNumber,
        int field2,
        int tempGamFileOffset,
        int objFixedFileOffset,
        int fieldE,
        int field12
    ) {
        ZoneNumber = zoneNumber;
        Field2 = field2;
        TempGamFileOffset = tempGamFileOffset;
        ObjFixedFileOffset = objFixedFileOffset;
        FieldE = fieldE;
        Field12 = field12;
    }

    public short ZoneNumber { get; }
    public int Field2 { get; }
    public int TempGamFileOffset { get; }
    public int ObjFixedFileOffset { get; }
    public int FieldE { get; }
    public int Field12 { get; }
}
