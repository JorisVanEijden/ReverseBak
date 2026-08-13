namespace GameData.Resources.Spells;

/// <summary>
/// Which overworld spell palette-effects are currently running, as the original's
/// <c>wPalEventMask</c> — one bit per effect, maintained by <c>spellfx_pal_event_mask_upd</c>.
///
/// <para>This is what a <c>timerType_Spell</c> entry in the timer pool actually does. It is
/// <b>not</b> an expiry action: <c>timerpool_tick</c> calls the hook on <i>every</i> tick, and the
/// bit is set for as long as the timer has time left and cleared on the tick it reaches zero —
/// which is the tick immediately before the entry is removed. A port that only runs a hook when a
/// timer fires never clears the bit at all, leaving the effect permanently "on".</para>
///
/// <para>Cast from the overworld, the palette spells (Dragon's Breath, Stardusk, Candle Glow and
/// the rest of <c>spellfx_cast_and_dispatch</c>'s cases) schedule a timer whose duration is
/// <c>effectParam * power * 30</c>, so the effect lasts in proportion to the power invested.</para>
/// </summary>
public static class SpellPaletteEvents {
    /// <summary>
    /// Effects the mask can hold, ids 0..8. The original indexes a nine-entry table and silently
    /// ignores anything else, so an out-of-range id leaves the mask untouched rather than throwing.
    /// </summary>
    public const int Count = 9;

    /// <summary>
    /// The bit for one effect. The shipped <c>g_aPalEventBitMask</c> table is written out
    /// longhand as {0x0001, 0x0002, … 0x0100}, but it is exactly <c>1 &lt;&lt; id</c> — worth
    /// knowing before anyone treats the table as an arbitrary mapping worth extracting.
    /// </summary>
    public static int BitFor(int eventId) =>
        eventId >= 0 && eventId < Count ? 1 << eventId : 0;

    /// <summary>
    /// Sets or clears one effect's bit, returning the new mask.
    /// </summary>
    /// <param name="remainingTime">
    /// The timer's remaining time <b>after</b> this tick's decrement, clamped at zero. Non-zero
    /// keeps the effect on; zero turns it off. Passing the pre-decrement value would hold every
    /// effect on for one tick too long.
    /// </param>
    public static int Apply(int mask, int eventId, long remainingTime) {
        int bit = BitFor(eventId);
        if (bit == 0) {
            return mask;
        }
        return remainingTime != 0 ? mask | bit : mask & ~bit;
    }
}
