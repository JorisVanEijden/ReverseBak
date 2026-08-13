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

    /// <summary>Fade rate passed to the fade-out before a switch.</summary>
    public const int FadeRate = 0x32;

    /// <summary>Ticks the original waits for the fade to finish before stopping the driver.</summary>
    public const int FadeWaitTicks = 0x15e;

    /// <summary>Volume a freshly started track is set to.</summary>
    public const int FullVolume = 0x7f;

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
