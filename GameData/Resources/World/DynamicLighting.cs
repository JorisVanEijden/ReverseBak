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

    /// <summary>
    /// The colours the blends pull toward, as VGA six-bit RGB (0-63).
    /// </summary>
    /// <remarks>
    /// Read from the binary rather than guessed — this was the spec's first open question. Two are
    /// worth a second look:
    /// <list type="bullet">
    ///   <item><b>The candle colour is a dark GREEN</b> (3, 23, 1), not the warm tone its name
    ///   suggests. It is the underground tint, so caves are cast green rather than firelit. The
    ///   name and the data disagree and the data wins; if a screenshot ever says otherwise, check
    ///   the byte order before changing this.</item>
    ///   <item>The item light (54, 44, 18) IS the warm one — bright amber, the tone a torch would
    ///   give. So the two are almost opposites of what the names imply.</item>
    /// </list>
    /// </remarks>
    public static class Colors {
        /// <summary>Dragon's breath, a desaturated blue-grey.</summary>
        public static readonly (int R, int G, int B) DragonsBreath = (10, 20, 23);

        /// <summary>Darkness, the target every darkening blend uses.</summary>
        public static readonly (int R, int G, int B) Black = (0, 0, 0);

        /// <summary>Light from a carried item — warm amber.</summary>
        public static readonly (int R, int G, int B) ItemLight = (54, 44, 18);

        /// <summary>The underground tint. Green, despite the name.</summary>
        public static readonly (int R, int G, int B) CandleLight = (3, 23, 1);

        /// <summary>Stardusk, a deep blue.</summary>
        public static readonly (int R, int G, int B) Stardusk = (6, 11, 33);
    }

    /// <summary>The colour a tint pulls toward.</summary>
    public static (int R, int G, int B) ColorOf(Tint tint) => tint switch {
        Tint.Candle => Colors.CandleLight,
        Tint.Stardusk => Colors.Stardusk,
        Tint.ItemLight => Colors.ItemLight,
        _ => Colors.Black,
    };

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

    /// <summary>Mode set while no zone palette is loaded; nothing is lit.</summary>
    public const int ModeOff = 0;

    /// <summary>Mode set whenever a zone palette is loaded.</summary>
    public const int ModeZone = 1;

    /// <summary>
    /// The wider mode, lighting almost the whole palette — what a screen with its own palette uses.
    /// </summary>
    /// <remarks>
    /// <b>The mode is really "how much of the palette belongs to this scene".</b> A zone shares the
    /// display with the interface and keeps 0-111 back; a screen that has queued its own palette
    /// claims everything from 16 up. Read that way the two values stop looking like magic numbers.
    /// </remarks>
    public const int ModeExtended = 2;

    /// <summary>Whether this mode lights anything.</summary>
    /// <remarks>Any mode other than 1 or 2 <b>returns before touching the palette</b>.</remarks>
    public static bool ModeLights(int mode) => mode == ModeZone || mode == ModeExtended;

    /// <summary>
    /// Whether a frame's pending palette goes through lighting at all.
    /// </summary>
    /// <remarks>
    /// <b>The mode is the on/off switch for the whole system, and it tracks the zone palette.</b>
    /// Loading one sets it to <see cref="ModeZone"/>; disposing them sets it back to
    /// <see cref="ModeOff"/>. So lighting is live exactly while a zone's palette is resident, and a
    /// palette queued with no zone loaded is applied raw.
    ///
    /// <para>The frame path also decides <i>when</i>: the lighting runs BEFORE the buffer swap and
    /// the resulting palette is applied AFTER it. Doing both on the same side of the swap is what
    /// produces the tearing the original is avoiding.</para>
    ///
    /// <para><b>A screen that brings its own palette SETS the mode and then CLEARS it — it does not
    /// save and restore one.</b> The xref pattern looks like save-set-restore and is not: the screen
    /// writes <see cref="ModeExtended"/> (or <see cref="ModeOff"/>) on the way in and
    /// <see cref="ModeOff"/> on the way out, with no read of the previous value anywhere. So
    /// whatever the mode was before, it is 0 afterwards, and the zone's lighting is only live again
    /// once its palette is reloaded.</para>
    ///
    /// <para>Which of the two it writes comes from a global flag rather than from the screen: set,
    /// and the screen turns lighting <i>off</i> for its duration; clear, and it uses
    /// <see cref="ModeExtended"/>. What that flag tracks is not established.</para>
    /// </remarks>
    public static bool FrameIsLit(bool palettePending, int mode) => palettePending && ModeLights(mode);

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
