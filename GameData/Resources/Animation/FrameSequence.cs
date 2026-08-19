namespace GameData.Resources.Animation;

using System.Collections.Generic;

/// <summary>
/// Finding a jump target in a cutscene's frames — what a <c>GotoFrame</c> resolves against.
/// </summary>
public static class FrameSequence {
    /// <summary>No frame carries the tag.</summary>
    public const int NotFound = -1;

    /// <summary>
    /// The index of the frame carrying a tag, or <see cref="NotFound"/>.
    /// </summary>
    /// <remarks>
    /// <b>A TAGGED FRAME IS A NORMAL FRAME THAT HAPPENS TO BE LABELLED.</b> The tag comes from a
    /// <c>TagFrame</c> command sitting among the frame's other commands — the extractor lifts its
    /// number onto <see cref="Frame.Tag"/> and leaves the frame's contents alone, and the frame
    /// still runs to the next end-of-frame marker. So a jump has to land ON that frame and execute
    /// it; resuming after it drops everything the target frame was going to do, which shows up as a
    /// scene missing a step rather than as a jump going wrong.
    ///
    /// <para><b>A tag nothing carries answers <see cref="NotFound"/> rather than zero</b>, because
    /// zero is a real frame index. A caller that treats "not found" as a position restarts the
    /// scene instead of reporting a broken jump.</para>
    /// </remarks>
    public static int IndexOfTag(IReadOnlyList<Frame> frames, int tag) {
        if (frames == null) {
            return NotFound;
        }

        for (var index = 0; index < frames.Count; index++) {
            if (frames[index]?.Tag == tag) {
                return index;
            }
        }

        return NotFound;
    }
}
