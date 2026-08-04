namespace ResourceExtraction.Tests.Dialog;

using System.Text.Json;
using GameData.Resources.Dialog.Actions;
using GameData.Resources.Layout;
using Xunit;

public class ResizeDialogActionTests {
    /// <summary>
    /// The per-entry resize and the style's own default area have to speak the same vocabulary,
    /// or the "replace wholesale" semantics of <c>dialog_getDialogArea</c> (0x485bc) could only be
    /// expressed by mixing an int rect into a <see cref="LayoutHint"/> component by component.
    /// </summary>
    [Fact]
    public void ToLayoutHint_ProducesTheFourIntsAsDesignFramePx() {
        // Asymmetric, non-round, no two components sharing a number: a hint built by copying the
        // wrong field cannot reproduce this set.
        var resize = new ResizeDialogAction { Left = 315, Top = 738, Width = 1129, Height = 402 };

        LayoutHint hint = resize.ToLayoutHint();

        Assert.Equal(LayoutLength.Px(315f), hint.Left);
        Assert.Equal(LayoutLength.Px(738f), hint.Top);
        Assert.Equal(LayoutLength.Px(1129f), hint.Width);
        Assert.Equal(LayoutLength.Px(402f), hint.Height);
    }

    /// <summary>
    /// The hint a resize produces must be a COMPLETE area, not a partial one that leans on
    /// whatever it replaced: absolute placement, top-left anchored, far edges unopinionated. That
    /// is what lets <c>ResolveArea</c> hand it back as a total replacement without merging.
    /// </summary>
    [Fact]
    public void ToLayoutHint_IsSelfContained_AbsoluteTopLeftWithNoFarEdgeOpinion() {
        LayoutHint hint = new ResizeDialogAction { Left = 315, Top = 738, Width = 1129, Height = 402 }
            .ToLayoutHint();

        Assert.Equal(LayoutPosition.Absolute, hint.Position);
        Assert.Equal(LayoutAnchor.TopLeft, hint.Anchor);
        Assert.Equal(LayoutLength.Auto, hint.Right);
        Assert.Equal(LayoutLength.Auto, hint.Bottom);
        Assert.Null(hint.Flow);
        Assert.Null(hint.Grid);
        Assert.Null(hint.Padding);
    }

    /// <summary>
    /// Every call must hand back its own hint. A cached/shared instance would let one dialog's
    /// layout tweak leak into the next entry that carries the same action object.
    /// </summary>
    [Fact]
    public void ToLayoutHint_ReturnsAFreshHintEachCall() {
        var resize = new ResizeDialogAction { Left = 315, Top = 738, Width = 1129, Height = 402 };

        Assert.NotSame(resize.ToLayoutHint(), resize.ToLayoutHint());
    }

    /// <summary>
    /// The four ints are extractor-emitted from DDX and already canonical
    /// (<c>CanonicalSpace.Apply(Dialog)</c>), so the committed <c>generated/DDX/*.json</c> shape
    /// must not change: the hint is a derived VIEW, not a serialized field.
    /// </summary>
    [Fact]
    public void SerializedShape_IsStillTheFourInts_WithNoLayoutHintField() {
        var resize = new ResizeDialogAction { Left = 315, Top = 738, Width = 1129, Height = 402 };

        string json = JsonSerializer.Serialize(resize);

        Assert.Equal("{\"Left\":315,\"Top\":738,\"Width\":1129,\"Height\":402}", json);
    }
}
