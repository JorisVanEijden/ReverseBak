namespace GameData.Resources.Audio;

/// <summary>
/// The world's ambient sound effects — <c>audio_ambient_tick</c> (<c>SRC/AUDIO/ENGINE/AUDIO.C</c>),
/// called once per pass of the world loop.
/// </summary>
/// <remarks>
/// <b>This is not music.</b> It is the occasional bird, drip or distant noise laid over whatever the
/// music is doing, and it comes from the SFX bank rather than the song list — so "the overworld has
/// no music" and "the overworld is silent" are different claims and only the first is true.
///
/// <para><b>Nothing here is a loop or a schedule.</b> Every tick rolls a die and usually does
/// nothing; there is no timer, no queue and no minimum gap. A port that plays ambience on a fixed
/// interval sounds mechanical in a way the original never does, and one that treats the roll as a
/// per-second chance gets a rate that depends on its own frame rate.</para>
/// </remarks>
public static class AmbientSound {
    /// <summary>Above ground, one tick in this many produces a sound.</summary>
    public const int AbovegroundOneIn = 0x6e;

    /// <summary>Underground, one tick in this many.</summary>
    /// <remarks>
    /// <b>Deliberately rarer than above ground</b> — 180 against 110 — so a dungeon is quieter as
    /// well as differently voiced. Sharing one rate loses that.
    /// </remarks>
    public const int UndergroundOneIn = 0xb4;

    /// <summary>How often a sound comes, as a one-in-N chance per tick.</summary>
    public static int OneIn(bool underground) =>
        underground ? UndergroundOneIn : AbovegroundOneIn;

    /// <summary>Whether this tick sounds at all. The roll fires only on zero.</summary>
    public static bool Fires(int chanceRoll) => chanceRoll == 0;

    /// <summary>The chapter that has no outdoor ambience at all.</summary>
    public const int SilentChapter = 8;

    /// <summary>The zone that has none either.</summary>
    public const int SilentZone = 6;

    /// <summary>
    /// Whether the world is silent here regardless of the roll.
    /// </summary>
    /// <remarks>
    /// <b>Two hard exclusions, and they are checked BEFORE the die.</b> Chapter 8 and zone 6 never
    /// make an outdoor sound. Underground is exempt from both — a dungeon in chapter 8 still drips —
    /// because the mode check comes first and the exclusions live only in the outdoor branch.
    /// </remarks>
    public static bool IsSilent(bool underground, int chapter, int zone) =>
        !underground && (chapter == SilentChapter || zone == SilentZone);

    /// <summary>The single sound a dungeon makes.</summary>
    public const int UndergroundSfx = 3;

    /// <summary>
    /// The story flag that changes what the outdoors sounds like.
    /// </summary>
    /// <remarks>
    /// <b>The ambient palette is story-dependent.</b> Before this flag the world offers one pair of
    /// sounds and after it another — so a port that hardcodes one set has the world sounding the same
    /// all game, and the change is invisible in the audio data because it lives in a flag test.
    /// </remarks>
    public const int MoodFlag = 0x753a;

    /// <summary>The zone with its own mix once <see cref="MoodFlag"/> is set.</summary>
    public const int DistinctZone = 2;

    /// <summary>Sound heard in <see cref="DistinctZone"/> about half the time after the flag.</summary>
    public const int DistinctZoneSfx = 0x85;

    /// <summary>The rarer of the two pre-flag sounds.</summary>
    public const int RarePreFlagSfx = 0x5a;

    /// <summary>The common pre-flag sound.</summary>
    public const int CommonPreFlagSfx = 0x33;

    /// <summary>Lowest id of the post-flag outdoor range.</summary>
    public const int PostFlagFirstSfx = 52;

    /// <summary>Highest id of that range, inclusive.</summary>
    public const int PostFlagLastSfx = 54;

    /// <summary>Base of the two-sound pair the distinct zone falls back to.</summary>
    public const int DistinctZonePairBase = 0x35;

    /// <summary>
    /// Which sound plays outdoors.
    /// </summary>
    /// <param name="percentRoll">A roll in <c>[0, 100)</c>.</param>
    /// <param name="pairRoll">A roll in <c>[0, 2)</c>.</param>
    /// <param name="rangeRoll">
    /// A roll already in <c>[<see cref="PostFlagFirstSfx"/>, <see cref="PostFlagLastSfx"/>]</c>.
    /// </param>
    /// <remarks>
    /// The percentage comparisons are <b>inclusive</b>, as everywhere else in this codebase: the
    /// rare pre-flag sound is <c>roll &lt;= 5</c>, so six outcomes in a hundred rather than five.
    /// </remarks>
    public static int PickAboveground(int zone, bool moodFlagSet,
        int percentRoll, int pairRoll, int rangeRoll) {
        if (!moodFlagSet) {
            return percentRoll <= 5 ? RarePreFlagSfx : CommonPreFlagSfx;
        }
        if (zone != DistinctZone) {
            return rangeRoll;
        }
        return percentRoll <= 50 ? DistinctZoneSfx : DistinctZonePairBase + pairRoll;
    }

    /// <summary>Lowest intensity an ambient sound is played at.</summary>
    public const int MinIntensity = 10;

    /// <summary>
    /// The loudest it gets — <b>different above and below ground</b>.
    /// </summary>
    /// <remarks>
    /// 63 outdoors against 59 underground. A small difference, and the only reason to keep it is that
    /// it is free: the ranges are two literals in the same function, and collapsing them is a change
    /// with nothing to gain.
    /// </remarks>
    public static int MaxIntensity(bool underground) => underground ? 59 : 63;

    /// <summary>The intensity range a sound is played at.</summary>
    public static (int Min, int Max) IntensityRange(bool underground) =>
        (MinIntensity, MaxIntensity(underground));
}
