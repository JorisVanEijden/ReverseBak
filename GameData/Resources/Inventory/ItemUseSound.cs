namespace GameData.Resources.Inventory;

/// <summary>
/// The sound an item makes when it is used — the tail of <c>itemuse_dispatch_on_target</c>
/// (ITEMUSE.C:490).
/// </summary>
/// <remarks>
/// <b>It is DATA, not a per-item rule.</b> The dispatch plays
/// <c>audio_sfx_play_n_times(rec-&gt;wUse_sfx &amp; 0xff, rec-&gt;wUse_sfx &gt;&gt; 8, 1)</c> — the cue is on
/// the item's own record, so 30 of the 138 shipped items carry one and none of them needed a call
/// site of its own. Our extractor already splits the packed word into
/// <see cref="Object.ObjectInfo.SoundId"/> and <see cref="Object.ObjectInfo.SoundRepeat"/>.
/// </remarks>
public static class ItemUseSound {
    /// <summary>
    /// Whether a use with this outcome makes its sound at all.
    /// </summary>
    /// <remarks>
    /// <b>Outcome 0 returns BEFORE the sound.</b> The tail is
    /// <c>if (outcome == 0) { dialog 0x1b7743; return result; }</c> and only then reaches
    /// <c>wUse_sfx</c> — so an item that did nothing is silent, which is what makes the cue mean
    /// "that worked" rather than "you clicked something".
    ///
    /// <para><see cref="ItemUseOutcome.NotPorted"/> is ours and is treated the same way: we do not
    /// know that anything happened, so claiming it with a sound would be worse than silence.</para>
    /// </remarks>
    public static bool Sounds(ItemUseOutcome outcome) =>
        outcome != ItemUseOutcome.NoEffect && outcome != ItemUseOutcome.NotPorted;

    /// <summary>
    /// How many times the cue is heard — <b>the stored count is EXTRA repeats</b>.
    /// </summary>
    /// <remarks>
    /// <c>audio_sfx_play_n_times(int sfx_id, int extra_repeats, int blocking)</c>, from the
    /// engine's own header. So the common stored value of <b>0 means play once</b>, not "do not
    /// play" — reading it as a count silences 27 of the 30 items that carry a sound. The Armorer's
    /// Hammer stores 2 (three strikes) and the Whetstone 1 (two passes), which is the whole reason
    /// the field is not a bool.
    /// </remarks>
    public static int TimesHeard(int soundRepeat) => soundRepeat < 0 ? 1 : soundRepeat + 1;
}
