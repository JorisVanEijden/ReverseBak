namespace GameData.Resources.Data;

public class SaveGameMovementData {
    public SaveGameMovementData(
        short isAutoTraveling,
        byte subTileStepCount,
        short tileBoundaryCrossed,
        int savedCameraZPosition
    ) {
        IsAutoTraveling = isAutoTraveling;
        SubTileStepCount = subTileStepCount;
        TileBoundaryCrossed = tileBoundaryCrossed;
        SavedCameraZPosition = savedCameraZPosition;
    }

    public short IsAutoTraveling { get; }
    public byte SubTileStepCount { get; }
    public short TileBoundaryCrossed { get; }
    public int SavedCameraZPosition { get; }
}
