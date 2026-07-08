namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// When a <see cref="PlayAudioAction"/> fires, relative to the dialog entry's action execution.
/// <c>ExecuteDialog</c> (@0x494bb) runs an entry's actions in three sequential passes, each
/// handling a different subset of action types; this value selects which pass plays the audio
/// (verified from the three PlayAudio checks in that function). In every pass the <c>AudioId</c>
/// picks the source: &lt; 1000 plays a sound effect (<c>audio_sound_play</c>), &gt;= 1000 plays a
/// song (<c>audio_song_sub_1505A</c>).
/// </summary>
public enum PlayAudioTiming {
    /// <summary>Pass 1 — the pre pass (text-variable setup), before the entry's main actions run.
    /// The default and by far the most common value.</summary>
    BeforeActions = 0,

    /// <summary>Pass 2 — the main action-dispatch pass, alongside the entry's gameplay effects.</summary>
    WithActions = 1,

    /// <summary>Pass 3 — the final pass (push-next-entry / set-return-value), after the main actions.</summary>
    AfterActions = 2,
}
