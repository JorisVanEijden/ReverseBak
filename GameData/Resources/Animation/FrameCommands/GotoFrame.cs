namespace GameData.Resources.Animation.FrameCommands;

/// <summary>
/// Set the specified frame as the next to execute.
/// </summary>
public class GotoFrame : FrameCommand {
    /// <summary>The <b>tag</b> of the frame to jump to (NOT a frame index — the runtime seeks the
    /// frame whose <see cref="Frame.Tag"/> equals this value). Kept for fidelity; de-indexed into
    /// <see cref="TargetKey"/>.</summary>
    public int NextFrame { get; set; }

    /// <summary>De-indexed jump target: the stable key of the tagged frame,
    /// <c>base:ttm:&lt;file&gt;:tag:&lt;NextFrame&gt;</c>. Resolves to a <see cref="Frame.Key"/>. See
    /// docs/re-notes/reference-inventory.md #6.</summary>
    public string TargetKey { get; set; } = "";

    public override string ToString() {
        return $"{nameof(GotoFrame)}({NextFrame});";
    }
}