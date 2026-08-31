namespace GameData.Resources.Audio;

/// <summary>
/// What changing the background music actually does — <c>audio_music_play</c>
/// (<c>SRC/AUDIO/ENGINE/AUDIO.C</c>).
///
/// <para>The function looks like "play this track" and is really "swap to this track and tell me
/// what was playing". That return value is the whole save-and-restore idiom the game uses around
/// screens: an interactive location stashes the world's music on entry and puts it back on exit.
/// </para>
/// </summary>
public static class MusicPlayback {
    /// <summary>
    /// <b>Ask without changing anything.</b> Passing this returns the current track and does
    /// nothing else — it is how callers read the music state, not a track id. Every "save the
    /// current music" site in the game is a call with this value.
    /// </summary>
    public const int QueryOnly = -999;

    /// <summary>Silence: stop what is playing and start nothing.</summary>
    public const int NoTrack = -1;

    /// <summary>
    /// The first MUSIC id. Anything at or above this is a track; below it is a sound effect.
    /// </summary>
    /// <remarks>
    /// <b>One id space, split by a threshold — not two.</b> <c>audio_play</c> (AUDIO.C) is the
    /// single entry point every caller uses, and its whole dispatch is
    /// <c>if (sound_id &gt;= 0x3e9) { audio_music_play(sound_id); return; }</c> before falling
    /// through to <c>audio_start_by_id</c>. So a TTM script's <c>PlaySound</c>, a combat item's cue
    /// and a spell's cue all go to the same routine and are sorted by value alone.
    ///
    /// <para><b>You do not LOAD a song.</b> <c>audio_sfx_register</c> and <c>audio_sfx_stop</c> both
    /// refuse ids at or above this, so the SFX archive holds only the range below it. That is why 19
    /// of the 42 shipped TTM scripts play an id nothing in the file loads — every chapter cutscene
    /// plays its track without a load, because loading one would be a no-op.</para>
    ///
    /// <para><b>And the two halves are gated differently:</b> the effect branch returns early when
    /// the engine's sound-effects preference is off, while music has already returned by then. A
    /// port that routes both through one "is audio enabled" check silences music with the SFX
    /// setting.</para>
    /// </remarks>
    public const int FirstMusicId = 0x3e9;

    /// <summary>Whether an id from the shared space names a music track rather than an effect.</summary>
    public static bool IsMusic(int soundId) => soundId >= FirstMusicId;

    /// <summary>Fade rate passed to the fade-out before a switch.</summary>
    public const int FadeRate = 0x32;

    /// <summary>Ticks the original waits for the fade to finish before stopping the driver.</summary>
    public const int FadeWaitTicks = 0x15e;

    /// <summary>Volume a freshly started track is set to.</summary>
    public const int FullVolume = 0x7f;

    /// <summary>
    /// How long the fade before a stop or a switch lasts, in seconds.
    /// </summary>
    /// <remarks>
    /// <b>Derived, not chosen.</b> <see cref="FadeWaitTicks"/> is how long the original waits for
    /// the driver's fade to finish, in the game's own tick unit, and
    /// <see cref="Config.DialogTextSpeed.TicksPerSecond"/> is what that unit is worth — the PIT
    /// frequency over the reload value. <c>PicklockDrop</c> converts a delay the same way.
    ///
    /// <para><see cref="FadeRate"/> is the rate handed to the DOS driver and has no meaning outside
    /// it, so a port cannot use it directly; the observable behaviour is "the outgoing track takes
    /// about this long to reach silence".</para>
    /// </remarks>
    public static double FadeSeconds => FadeWaitTicks / Config.DialogTextSpeed.TicksPerSecond;

    /// <summary>
    /// The outgoing track's volume part-way through the fade, as a fraction of
    /// <see cref="FullVolume"/> — 1 at the start, 0 at or past <see cref="FadeSeconds"/>.
    /// </summary>
    public static double FadeVolumeFractionAt(double elapsedSeconds) {
        if (elapsedSeconds <= 0) {
            return 1.0;
        }
        double total = FadeSeconds;
        if (total <= 0 || elapsedSeconds >= total) {
            return 0.0;
        }
        return 1.0 - (elapsedSeconds / total);
    }

    /// <summary>What a request resolves to.</summary>
    public enum MusicAction {
        /// <summary>Nothing happens — a query, a repeat of what is already playing, or no driver.</summary>
        None,

        /// <summary>Fade and stop the current track, start nothing.</summary>
        Stop,

        /// <summary>Fade and stop the current track, then load and start the requested one.</summary>
        Switch,
    }

    /// <summary>The decision, plus the track the caller should remember.</summary>
    public readonly struct MusicChange {
        public MusicChange(MusicAction action, int previousTrack) {
            Action = action;
            PreviousTrack = previousTrack;
        }

        public MusicAction Action { get; }

        /// <summary>
        /// What was playing before. <b>Returned in every case, including when nothing changes</b> —
        /// that is what makes a query work, and what a caller stashes to restore later.
        /// </summary>
        public int PreviousTrack { get; }
    }

    /// <summary>
    /// Resolves a music request.
    /// </summary>
    /// <param name="requestedTrack">The track wanted, or <see cref="QueryOnly"/> / <see cref="NoTrack"/>.</param>
    /// <param name="currentTrack">What is playing now; <see cref="NoTrack"/> for silence.</param>
    /// <param name="hasSoundDriver">False when no driver is configured — every request is inert.</param>
    /// <remarks>
    /// <b>Re-requesting the track already playing does nothing.</b> It is not a restart: the guard
    /// sits above the fade, so walking back into a zone whose music is already going does not
    /// interrupt it. A port that restarts on every zone entry would stutter the music at every
    /// boundary.
    /// </remarks>
    public static MusicChange Resolve(int requestedTrack, int currentTrack,
        bool hasSoundDriver = true) {
        if (!hasSoundDriver || requestedTrack == QueryOnly || requestedTrack == currentTrack) {
            return new MusicChange(MusicAction.None, currentTrack);
        }
        return new MusicChange(
            requestedTrack == NoTrack ? MusicAction.Stop : MusicAction.Switch, currentTrack);
    }

    /// <summary>
    /// Whether the outgoing track needs fading and stopping. False when nothing is playing, which
    /// is why the first track of a session starts without a preceding fade.
    /// </summary>
    public static bool NeedsFadeOut(int currentTrack) => currentTrack != NoTrack;

    /// <summary>
    /// Whether the audible half of a switch happens at all.
    ///
    /// <para>With music turned off in preferences the original still does the bookkeeping — it
    /// stops, loads the chunk and records the new current track — and simply never starts it
    /// playing. So the track a later query reports is the one that <i>would</i> be playing, and
    /// turning music back on mid-game does not resync it.</para>
    /// </summary>
    public static bool IsAudible(bool musicEnabledInPreferences) => musicEnabledInPreferences;
}
