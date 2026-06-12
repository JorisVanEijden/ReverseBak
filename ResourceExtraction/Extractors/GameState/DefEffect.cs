namespace ResourceExtraction.Extractors.GameState;

using GameData.Resources.GameState;

/// <summary>
/// DEF trigger payloads set (ENAB) or clear (DISA) a single global flag when
/// they fire. This maps that write to the shared <see cref="Effect"/> vocabulary.
/// </summary>
public static class DefEffect {
    /// <summary>The effect of a DEF entry whose <paramref name="key"/> flag is set/cleared on fire; null when key 0 (no-op).</summary>
    public static Effect? ForKey(int key, bool set) {
        if (key == 0) {
            return null;
        }
        return new SetFlagEffect { Flag = key, Set = set };
    }
}
