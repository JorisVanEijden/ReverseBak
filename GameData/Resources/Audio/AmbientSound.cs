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

    /// <summary>The single sound a dungeon makes: a water drip.</summary>
    /// <remarks>
    /// Confirmed twice over — <c>audio_ambient_tick</c> plays literal 3, and
    /// <c>audio_unload_world_sounds</c> frees literal 3 on the underground branch where the outdoor
    /// branch frees its bird list. IDA's sound enum names 3 <c>sound_drip</c>.
    /// </remarks>
    public const int UndergroundSfx = 3;   // sound_drip

    /// <summary>
    /// The story flag that changes what the outdoors sounds like.
    /// </summary>
    /// <remarks>
    /// <b>The ambient palette is story-dependent.</b> Before this flag the world offers one pair of
    /// sounds and after it another — so a port that hardcodes one set has the world sounding the same
    /// all game, and the change is invisible in the audio data because it lives in a flag test.
    /// </remarks>
    public const int MoodFlag = 0x753a;

    /// <summary>
    /// <b>What the flag means is NOT established, and the sounds are only suggestive.</b>
    /// </summary>
    /// <remarks>
    /// Named against IDA's sound enum, the two sets are crickets-and-an-owl before, and birdsong
    /// after — which reads as night giving way to day. That reading is not supported: 0x753a is an
    /// ordinary save-state event flag, it is written nowhere in the reconstructed sources, and its
    /// only other reader gates an NPC interaction branch, which a clock would fit oddly.
    ///
    /// <para>So the sound sets are recorded as what they are and the flag is left unnamed. Calling
    /// it a day/night flag would be a guess dressed as a finding, and a port that wired it to a clock
    /// would change when NPCs can be interacted with.</para>
    /// </remarks>
    public static bool MoodFlagMeaningIsKnown => false;

    /// <summary>The zone with its own mix once <see cref="MoodFlag"/> is set.</summary>
    public const int DistinctZone = 2;

    /// <summary>Sound heard in <see cref="DistinctZone"/> about half the time after the flag: gulls.</summary>
    /// <remarks>
    /// <b>Zone 2 is the coast.</b> Independently confirmed by the unload path, which frees
    /// <c>sound_gulls</c> for <c>currentZoneNumber == 2</c> and for no other zone — so the tick's
    /// special case and the bank's special case are the same zone for the same reason.
    /// </remarks>
    public const int DistinctZoneSfx = 0x85;   // sound_gulls

    /// <summary>The rarer of the two pre-flag sounds: an owl.</summary>
    public const int RarePreFlagSfx = 0x5a;   // sound_hoot

    /// <summary>The common pre-flag sound: crickets.</summary>
    public const int CommonPreFlagSfx = 0x33;   // sound_crickets

    /// <summary>Lowest id of the post-flag outdoor range: the first birdsong.</summary>
    public const int PostFlagFirstSfx = 52;   // sound_birds1

    /// <summary>Highest id of that range, inclusive: the third birdsong.</summary>
    public const int PostFlagLastSfx = 54;   // sound_birds3

    /// <summary>Base of the two-sound pair the distinct zone falls back to: the second birdsong.</summary>
    public const int DistinctZonePairBase = 0x35;   // sound_birds2, +1 = sound_birds3

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
