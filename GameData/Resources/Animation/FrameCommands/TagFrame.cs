namespace GameData.Resources.Animation.FrameCommands;

/// <summary>
/// Names the frame it sits in — TTM opcodes <c>0x1101</c> and <c>0x1111</c>.
/// </summary>
/// <remarks>
/// The extractor lifts <see cref="TagNumber"/> onto <c>Frame.Tag</c> and leaves the command where
/// it is, which is what makes a <c>GotoFrame</c> a TAG reference rather than an index — see
/// <see cref="FrameSequence.IndexOfTag"/>.
/// </remarks>
public class TagFrame : FrameCommand {
    public int TagNumber { get; set; }

    /// <summary>
    /// Which of the two tag opcodes this was: true for <c>0x1111</c>, false for <c>0x1101</c>.
    /// </summary>
    /// <remarks>
    /// <b>The NAME is honest and the difference is genuinely unestablished.</b> Both opcodes read
    /// the same single word and produce the same tag; what the second one means is not known, so
    /// the flag records which was written rather than claiming an interpretation. Renaming it to
    /// something plausible would turn an open question into a false answer — the whole reason
    /// provisional names carry a marker in this project.
    ///
    /// <para>It is preserved rather than dropped so the distinction survives a round-trip: a
    /// writer that emitted 0x1101 for every tag would silently rewrite whichever scripts used the
    /// other one.</para>
    /// </remarks>
    public bool UnknownBool { get; set; }

    public override string ToString() {
        return $"{nameof(TagFrame)}({TagNumber}, {UnknownBool}) ;";
    }
}
