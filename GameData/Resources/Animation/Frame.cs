namespace GameData.Resources.Animation;

using GameData.Resources.Animation.FrameCommands;

public record Frame {
    public List<FrameCommand> Commands { get; set; } = [];
    public int? Tag { get; set; }

    /// <summary>Stable content-graph key of this frame as a jump target, when it is one:
    /// <c>base:ttm:&lt;file&gt;:tag:&lt;Tag&gt;</c> (empty for untagged frames, which are only reached
    /// sequentially). A <see cref="FrameCommands.GotoFrame"/> resolves to this key. See
    /// docs/re-notes/reference-inventory.md #6.</summary>
    public string Key { get; set; } = "";
}