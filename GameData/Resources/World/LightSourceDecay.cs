namespace GameData.Resources.World;

/// <summary>
/// How a light source's level changes as its timer runs down — IDA <c>updateLightSources</c>
/// (seg031 @0x2d221).
///
/// <para><b>Every level is the square of the minutes left.</b> Not a linear burn. Said carefully,
/// because the square cuts both ways: the <i>absolute</i> drop per minute is largest just after the
/// source leaves its steady level, while the <i>proportional</i> loss bites at the end — three
/// minutes left is under a fifth of full, and the last minute is a fiftieth. It is the proportional
/// part a player sees, which is why it reads as a torch guttering out.</para>
/// </summary>
public static class LightSourceDecay {
    /// <summary>The four things that carry a light timer.</summary>
    public enum Source {
        /// <summary>A lit item in the party's hands.</summary>
        Item = 0,

        /// <summary>The dragon's-breath effect.</summary>
        DragonsBreath = 1,

        /// <summary>The candle-glow spell, which is what lights caves.</summary>
        CandleGlow = 2,

        /// <summary>Stardusk.</summary>
        Stardusk = 3,
    }

    /// <summary>
    /// Minutes at and above which a source is at its steady level rather than fading.
    /// </summary>
    public const int SteadyFromMinutes = 8;

    /// <summary>
    /// The level a steady source sits at.
    /// </summary>
    /// <remarks>
    /// <b>Seven squared, not eight.</b> A source with plenty of time left is pinned one step below
    /// where the curve would put it, so nothing ever reaches the light scale's ceiling of 64 from
    /// burning alone.
    /// </remarks>
    public const int SteadyLevel = 49;

    /// <summary>Dragon's breath's level at the moment its timer expires.</summary>
    public const int DragonsBreathPeak = 64;

    /// <summary>Dragon's breath's dim level while it still has time to run.</summary>
    public const int DragonsBreathIdle = 8;

    /// <summary>
    /// The light level a source is at with this many minutes left.
    /// </summary>
    /// <param name="flickerBit">
    /// The low bit of a fresh random draw. Only <see cref="Source.DragonsBreath"/> uses it.
    /// </param>
    /// <remarks>
    /// <b>Three of the four fade the same way and dragon's breath does the opposite.</b> An item, a
    /// candle and stardusk all decay — steady at <see cref="SteadyLevel"/> until eight minutes
    /// remain, then <c>minutes²</c> the rest of the way down, so six minutes left is still about
    /// three-quarters of full and the final minute is a fiftieth of it.
    ///
    /// <para>Dragon's breath instead <b>builds</b>: it flickers between 8 and 9 while it has time
    /// left, and over its last eight minutes ramps up as <c>(8 − minutes)²</c> to reach
    /// <see cref="DragonsBreathPeak"/> exactly when it expires. Porting it as another decaying
    /// source would run the effect backwards and lose its whole shape.</para>
    /// </remarks>
    public static int LevelFor(Source source, int minutesRemaining, int flickerBit = 0) {
        if (minutesRemaining < 0) {
            minutesRemaining = 0;
        }

        if (source == Source.DragonsBreath) {
            if (minutesRemaining >= SteadyFromMinutes) {
                return DragonsBreathIdle + (flickerBit & 1);
            }
            if (minutesRemaining == 0) {
                return DragonsBreathPeak;
            }
            int grown = SteadyFromMinutes - minutesRemaining;

            return grown * grown;
        }

        return minutesRemaining >= SteadyFromMinutes
            ? SteadyLevel
            : minutesRemaining * minutesRemaining;
    }

    /// <summary>
    /// Whether reaching zero on this source's timer also spends a charge on every lit item.
    /// </summary>
    /// <remarks>
    /// Only the item timer does this, and only on the tick where the remaining time is <b>exactly</b>
    /// zero — so the burn-down is charged once, not every tick afterwards.
    /// </remarks>
    public static bool SpendsItemChargesAt(Source source, long remainingTime) =>
        source == Source.Item && remainingTime == 0;

    /// <summary>
    /// Every update raises the "recompute the palette" flag, whichever source moved.
    /// </summary>
    public static bool AlwaysRequestsRelight => true;
}
