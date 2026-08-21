namespace GameData.Resources.Animation.FrameCommands;

/// <summary>
/// TTM <c>0x4004</c> — set the drawing clip rectangle.
/// </summary>
/// <remarks>
/// *** <see cref="Width"/> AND <see cref="Height"/> ARE MAXIMUM COORDINATES, NOT EXTENTS. ***
/// The original writes the four parameters straight into
/// <c>clip.xmin / clip.ymin / clip.xmax / clip.ymax</c> (canassa SRC/SCRIPT/TTM.C, case 0x4004), and
/// the bounds are inclusive. The names are ours and they are wrong; they are kept because the
/// extracted JSON in <c>generated/TTM/</c> carries them, and renaming would churn every one of
/// those files for a comment's worth of clarity.
///
/// <para><b>This type deliberately does NOT implement <c>IArea</c></b>, although it did until
/// 2026-08-22. Every generic <c>IArea</c> consumer — <c>Drawing.CopyArea</c>,
/// <c>Drawing.FillArea</c>, <c>Drawing.DrawBorder</c>, <c>DrawingUtils.GetBorderRects</c> — reads
/// Width/Height as extents, so handing one of these to any of them would clip or fill a region
/// running to <c>x + xmax</c> instead of to <c>xmax</c>. Nothing did, but the interface advertised a
/// contract this type does not honour, and removing it makes the mistake unavailable rather than
/// merely undocumented.</para>
///
/// <para>The consumer that IS correct is <c>DrawingUtils.CalculateRects</c>, which reads the Rect's
/// width/height slots as xMax/yMax and adds one for the inclusive bound.</para>
/// </remarks>
public class SetClipArea : FrameCommand {
    /// <summary>Left edge — <c>clip.xmin</c>.</summary>
    public int X { get; set; }

    /// <summary>Top edge — <c>clip.ymin</c>.</summary>
    public int Y { get; set; }

    /// <summary><b>Right edge (<c>clip.xmax</c>), inclusive — not a width.</b></summary>
    public int Width { get; set; }

    /// <summary><b>Bottom edge (<c>clip.ymax</c>), inclusive — not a height.</b></summary>
    public int Height { get; set; }

    public override string ToString() {
        return $"{nameof(SetClipArea)}({X}, {Y}, {Width}, {Height});";
    }
}