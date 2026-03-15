namespace GameData.Resources.Data;

public class SaveGameTimedEffectData {
    public SaveGameTimedEffectData(
        short activeTypeFlag,
        short attributeBitmask,
        short modifierValue,
        int startTime,
        int expiryTime
    ) {
        ActiveTypeFlag = activeTypeFlag;
        AttributeBitmask = attributeBitmask;
        ModifierValue = modifierValue;
        StartTime = startTime;
        ExpiryTime = expiryTime;
    }

    public short ActiveTypeFlag { get; }
    public short AttributeBitmask { get; }
    public short ModifierValue { get; }
    public int StartTime { get; }
    public int ExpiryTime { get; }

    public bool IsActive { get => ActiveTypeFlag != 0; }
}
