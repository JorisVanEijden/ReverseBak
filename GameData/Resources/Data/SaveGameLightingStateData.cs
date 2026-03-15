namespace GameData.Resources.Data;

public class SaveGameLightingStateData {
    public SaveGameLightingStateData(
        short activeSpellTimerFlags,
        short partyMember,
        short lastSpellSymbolFile,
        short lightNeedsUpdate,
        short previousDayLightLevel,
        short currentDaylightLevel,
        short itemLightLevel,
        short candleglowLightLevel,
        short starduskLightLevel,
        short dragonsbreathLightLevel
    ) {
        ActiveSpellTimerFlags = activeSpellTimerFlags;
        PartyMember = partyMember;
        LastSpellSymbolFile = lastSpellSymbolFile;
        LightNeedsUpdate = lightNeedsUpdate;
        PreviousDayLightLevel = previousDayLightLevel;
        CurrentDaylightLevel = currentDaylightLevel;
        ItemLightLevel = itemLightLevel;
        CandleglowLightLevel = candleglowLightLevel;
        StarduskLightLevel = starduskLightLevel;
        DragonsbreathLightLevel = dragonsbreathLightLevel;
    }

    public short ActiveSpellTimerFlags { get; }
    public short PartyMember { get; }
    public short LastSpellSymbolFile { get; }
    public short LightNeedsUpdate { get; }
    public short PreviousDayLightLevel { get; }
    public short CurrentDaylightLevel { get; }
    public short ItemLightLevel { get; }
    public short CandleglowLightLevel { get; }
    public short StarduskLightLevel { get; }
    public short DragonsbreathLightLevel { get; }
}
