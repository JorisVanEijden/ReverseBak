namespace GameData.Resources.Dialog;

using System.Collections.Generic;

/// <summary>
/// The two vine corner pieces the full-screen dialog is framed with —
/// <c>dialog_DrawChrome</c> @0x48864.
/// </summary>
/// <remarks>
/// <b>One sprite, drawn twice, the second rotated.</b> Both blits pass INVSHP2.BMX image 9; the
/// second passes <c>bitmapFlags 3</c> = VerticalFlip | HorizontalFlip, which is a 180° turn rather
/// than a mirror, so the same corner piece serves opposite corners.
///
/// <para><b>Only dialog type 6 gets them.</b> The branch is
/// <c>dec ax; cmp ax, 5; jnz return</c> at 0x4886c — i.e. exactly
/// <see cref="DialogType.PlainFullScreen"/>. Every other style returns before it.</para>
///
/// <para><b>The coordinates are SCREEN positions, not panel-relative.</b> The original hands them
/// straight to <c>drawBitmapAt</c>, which draws in screen space, and one of them is deliberately
/// NEGATIVE — the lower piece hangs off the left edge. Placing these inside the dialog panel would
/// clip that overhang and move both pieces with the panel, neither of which the original does.</para>
/// </remarks>
public static class DialogVineCorners {
    /// <summary>The image set both pieces come from.</summary>
    public const string IconSet = "INVSHP2.BMX";

    /// <summary>Sub-image index of the corner piece.</summary>
    public const int ImageIndex = 9;

    /// <summary>The dialog type that gets the vines, and the only one.</summary>
    public const DialogType DecoratedType = DialogType.PlainFullScreen;

    /// <summary>One placement of the corner piece.</summary>
    /// <param name="X">Screen x in design-frame px; may be negative.</param>
    /// <param name="Y">Screen y in design-frame px.</param>
    /// <param name="Rotated">Whether the piece is turned 180° (both axes flipped).</param>
    public readonly record struct Placement(int X, int Y, bool Rotated);

    /// <summary>
    /// Where the two pieces go, in design-frame px.
    /// </summary>
    /// <remarks>
    /// VGA (-4, 131) unflipped and (234, 3) flipped, scaled x5 across and x6 down. They sit at
    /// opposite corners — lower-left and upper-right — rather than on the same edge.
    /// </remarks>
    public static readonly IReadOnlyList<Placement> Placements = new[] {
        new Placement(-4 * 5, 131 * 6, Rotated: false),
        new Placement(234 * 5, 3 * 6, Rotated: true),
    };

    /// <summary>Whether an entry's resolved style is the one the vines decorate.</summary>
    public static bool DecoratesStyle(int effectiveStyleId) =>
        effectiveStyleId == (int)DecoratedType;
}
