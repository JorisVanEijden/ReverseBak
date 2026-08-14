namespace GameData.Resources.World;

/// <summary>
/// What the lighting blend is asked to do — IDA <c>ApplyDynamicLighting</c> (seg031 @0x2cdd9).
///
/// <para>The decisions only: which tint (if any), how strongly to apply it, how far to darken, and
/// which palette entries are in scope. The blending itself belongs to whatever draws.</para>
/// </summary>
public static class DynamicLighting {
    /// <summary>Darkening factor floor — the same night floor <see cref="DaylightLevel"/> uses.</summary>
    public const int MinimumLight = DaylightLevel.Night;

    /// <summary>Darkening factor ceiling.</summary>
    public const int MaximumLight = DaylightLevel.Day;

    /// <summary>
    /// Tint strength a candle is applied at underground — a <b>constant</b>, not a time-of-day value.
    /// </summary>
    public const int CandleTintStrength = 50;

    /// <summary>Lowest palette entry lit in mode 1; everything below it is passed through untouched.</summary>
    public const int Mode1FirstLitEntry = 112;

    /// <summary>Lowest palette entry lit in mode 2.</summary>
    public const int Mode2FirstLitEntry = 16;

    /// <summary>Highest palette entry lit, in either mode.</summary>
    public const int LastLitEntry = 255;

    /// <summary>Which tint the scene gets, if any.</summary>
    public enum Tint {
        /// <summary>No tint — only darkening.</summary>
        None,

        /// <summary>Candle warmth, underground only.</summary>
        Candle,

        /// <summary>Stardusk.</summary>
        Stardusk,

        /// <summary>Light from a carried item.</summary>
        ItemLight,
    }

    /// <summary>The lighting the scene is drawn with.</summary>
    public readonly struct Lighting {
        public Lighting(Tint tint, int tintStrength, int light, int firstLitEntry) {
            Tint = tint;
            TintStrength = tintStrength;
            Light = light;
            FirstLitEntry = firstLitEntry;
        }

        /// <summary>Which colour the palette is pulled toward before darkening.</summary>
        public Tint Tint { get; }

        /// <summary>How strongly. Ignored when <see cref="AppliesTint"/> is false.</summary>
        public int TintStrength { get; }

        /// <summary>
        /// How light the scene ends up, clamped to <see cref="MinimumLight"/>..<see cref="MaximumLight"/>.
        /// This is the factor the darkening blend uses.
        /// </summary>
        public int Light { get; }

        /// <summary>First palette entry in scope; everything below is passed through untouched.</summary>
        public int FirstLitEntry { get; }

        /// <summary>
        /// Whether the tint blend happens at all.
        /// </summary>
        /// <remarks>
        /// <b>A tint at full strength is skipped, not applied.</b> The guard is
        /// <c>strength &lt; 64</c>, and above ground the strength comes from the time-of-day curve —
        /// which is exactly 64 through the middle of the day. So <b>a light source tints nothing at
        /// noon</b> and only starts to show as evening comes on. Underground the strength is the
        /// constant 50, so a candle always tints.
        /// </remarks>
        public bool AppliesTint => Tint != Tint.None && TintStrength < MaximumLight;
    }

    /// <summary>Whether this mode lights anything.</summary>
    /// <remarks>Any mode other than 1 or 2 <b>returns before touching the palette</b>.</remarks>
    public static bool ModeLights(int mode) => mode == 1 || mode == 2;

    /// <summary>The first palette entry a mode lights.</summary>
    /// <remarks>
    /// Answers the spec's question about which entries are protected: everything <i>below</i> this
    /// is copied through from the destination palette untouched — 0-111 in mode 1, 0-15 in mode 2.
    /// </remarks>
    public static int FirstLitEntry(int mode) => mode == 2 ? Mode2FirstLitEntry : Mode1FirstLitEntry;

    /// <summary>
    /// Work out the lighting for a moment.
    /// </summary>
    /// <param name="underground">Whether the zone is the enclosed kind.</param>
    /// <param name="daylight">The time-of-day level — <see cref="DaylightLevel.At"/>.</param>
    /// <remarks>
    /// <b>Underground ignores both the clock and stardusk.</b> The daylight term and the stardusk
    /// term are added only on the above-ground path; underground the light is
    /// <c>candle + 15 + itemLight</c>. So time of day makes no difference at all below ground —
    /// a remake that dims a cave at night is inventing a mechanic.
    ///
    /// <para>Above ground the tint is chosen by <b>priority, not by sum</b>: stardusk if there is
    /// any, otherwise item light, otherwise none. Both sources still count toward the darkening
    /// term whichever is chosen, so having both is brighter than having one — but only one colour
    /// ever shows.</para>
    /// </remarks>
    public static Lighting Resolve(bool underground, int mode, int daylight,
        int candleLight, int starduskLight, int itemLight, int tintStrengthFromClock) {
        Tint tint;
        var tintStrength = 0;
        int light;

        if (underground) {
            tint = candleLight > 0 ? Tint.Candle : Tint.None;
            tintStrength = CandleTintStrength;
            light = candleLight + MinimumLight + itemLight;
        } else {
            if (starduskLight > 0) {
                tint = Tint.Stardusk;
                tintStrength = tintStrengthFromClock;
            } else if (itemLight > 0) {
                tint = Tint.ItemLight;
                tintStrength = tintStrengthFromClock;
            } else {
                tint = Tint.None;
            }
            light = daylight + starduskLight + itemLight;
        }

        if (light < MinimumLight) {
            light = MinimumLight;
        } else if (light > MaximumLight) {
            light = MaximumLight;
        }

        return new Lighting(tint, tintStrength, light, FirstLitEntry(mode));
    }

    /// <summary>
    /// The night floor to pass <see cref="DaylightLevel.WithFloor"/> for a tint's strength.
    /// </summary>
    public static int TintFloorFor(Tint tint) => tint switch {
        Tint.Stardusk => DaylightLevel.StarduskFloor,
        Tint.ItemLight => DaylightLevel.ItemLightFloor,
        _ => DaylightLevel.Night,
    };

    /// <summary>
    /// <b>Dragon's breath is applied on its own, before and regardless of everything else.</b>
    /// </summary>
    /// <remarks>
    /// It is not one of the tints and does not compete with them: its blend runs first, from the
    /// destination palette, whatever the zone or the time. A level of zero simply blends by nothing.
    /// </remarks>
    public static bool DragonsBreathIsIndependent => true;
}
