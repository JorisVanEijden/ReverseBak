namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// How a <see cref="PlayAudioAction"/>'s id picks what to play — <c>ExecuteDialog</c> @0x494bb.
/// </summary>
/// <remarks>
/// The dialog handler branches on the id alone: below <see cref="FirstSongId"/> it is a sound
/// effect (<c>audio_sfx_play_n_times(id, 0, 1)</c> — once, non-blocking), at or above it a music
/// track (<c>audio_music_play(id)</c>). The same two-way test appears at all three of the
/// function's <see cref="PlayAudioTiming"/> passes.
/// </remarks>
public static class DialogAudio {
    /// <summary>
    /// Ids below this play NOTHING, and that is specific to the build this port targets.
    /// </summary>
    /// <remarks>
    /// <b>The guard exists only in the CD release.</b> <c>audio_sfx_play_n_times</c> (AUDIO.C:409)
    /// opens with <c>#ifdef V102CD / if (sfx_id &lt; 1) return 0;</c> — the floppy build has no such
    /// test and would fall through to a table lookup. We target 1.02 CD, so the guard is ours.
    ///
    /// <para>It is not a nicety: <b>157 of the 779 shipped PlayAudio actions carry id 0</b>, easily
    /// the most common single value, and there is no sound 0 in the extracted corpus (it starts at
    /// 1). Playing them would be 157 misses per playthrough; skipping them is what the target build
    /// does.</para>
    /// </remarks>
    public const int FirstSoundId = 1;

    /// <summary>At and above this the id is a music track rather than a sound effect.</summary>
    public const int FirstSongId = 1000;

    /// <summary>What an id means: nothing, a sound effect, or a song.</summary>
    public enum Kind {
        /// <summary>Below <see cref="FirstSoundId"/> — the CD build plays nothing.</summary>
        Silent,

        /// <summary>A sound effect, played once.</summary>
        Sound,

        /// <summary>A music track.</summary>
        Song,
    }

    /// <summary>Which of the three an id names.</summary>
    public static Kind KindOf(int audioId) =>
        audioId < FirstSoundId ? Kind.Silent
        : audioId < FirstSongId ? Kind.Sound
        : Kind.Song;
}
