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

    /// <summary>
    /// <b>This is <c>wPalEventMask</c></b> — the bitmask of running overworld spell palette
    /// effects, one bit per effect (<see cref="Spells.SpellPaletteEvents"/>).
    /// </summary>
    /// <remarks>
    /// It sits at the head of this block only because the block starts immediately after the timer
    /// pool; <c>gstate.inc</c> lists <c>wPalEventMask</c>, <c>nSpellMenuCasterSlot</c>,
    /// <c>nSpellMenuPreselect</c> and <c>nPalFadeDirty</c> there, which are the four fields before
    /// <see cref="PreviousDayLightLevel"/>. So the first three are not lighting state at all, and
    /// the name of this one hid the fact that the save already carries the palette mask.
    /// </remarks>
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
