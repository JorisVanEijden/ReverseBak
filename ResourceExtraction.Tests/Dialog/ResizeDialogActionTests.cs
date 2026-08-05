namespace ResourceExtraction.Tests.Dialog;

using System.Text.Json;
using GameData.Resources.Dialog.Actions;
using GameData.Resources.Layout;
using Xunit;

public class ResizeDialogActionTests {
    /// <summary>
    /// The per-entry resize and the style's own default area have to speak the same vocabulary,
    /// or the "replace wholesale" semantics of <c>dialog_getDialogArea</c> (0x485bc) could only be
    /// expressed by mixing a rect into a <see cref="LayoutHint"/> component by component.
    /// </summary>
    [Fact]
    public void ToLayoutHint_ProducesTheFourLengthsAsGiven() {
        // Asymmetric, non-round, no two components sharing a number: a hint built by copying the
        // wrong field cannot reproduce this set.
        var resize = new ResizeDialogAction {
            Left = LayoutLength.Px(315f), Top = LayoutLength.Px(738f),
            Width = LayoutLength.Px(1129f), Height = LayoutLength.Px(402f)
        };

        LayoutHint hint = resize.ToLayoutHint();

        Assert.Equal(LayoutLength.Px(315f), hint.Left);
        Assert.Equal(LayoutLength.Px(738f), hint.Top);
        Assert.Equal(LayoutLength.Px(1129f), hint.Width);
        Assert.Equal(LayoutLength.Px(402f), hint.Height);
    }

    /// <summary>
    /// <see cref="ToLayoutHint"/> is a plain wrap now that the fields carry their own units — it
    /// must hand back whatever unit each field holds, not silently coerce to px. This is the
    /// override author's entry point: a percent-valued resize (which the extractor itself never
    /// produces — see the class doc comment) must reach the hint as percent.
    /// </summary>
    [Fact]
    public void ToLayoutHint_PreservesAMixOfPxAndPercentUnits() {
        var resize = new ResizeDialogAction {
            Left = LayoutLength.Percent(12.5f), Top = LayoutLength.Px(738f),
            Width = LayoutLength.Percent(64f), Height = LayoutLength.Px(402f)
        };

        LayoutHint hint = resize.ToLayoutHint();

        Assert.Equal(LayoutLength.Percent(12.5f), hint.Left);
        Assert.Equal(LayoutLength.Px(738f), hint.Top);
        Assert.Equal(LayoutLength.Percent(64f), hint.Width);
        Assert.Equal(LayoutLength.Px(402f), hint.Height);
    }

    /// <summary>
    /// The hint a resize produces must be a COMPLETE area, not a partial one that leans on
    /// whatever it replaced: absolute placement, top-left anchored, far edges unopinionated. That
    /// is what lets <c>ResolveArea</c> hand it back as a total replacement without merging.
    /// </summary>
    [Fact]
    public void ToLayoutHint_IsSelfContained_AbsoluteTopLeftWithNoFarEdgeOpinion() {
        LayoutHint hint = new ResizeDialogAction {
            Left = LayoutLength.Px(315f), Top = LayoutLength.Px(738f),
            Width = LayoutLength.Px(1129f), Height = LayoutLength.Px(402f)
        }.ToLayoutHint();

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
        var resize = new ResizeDialogAction {
            Left = LayoutLength.Px(315f), Top = LayoutLength.Px(738f),
            Width = LayoutLength.Px(1129f), Height = LayoutLength.Px(402f)
        };

        Assert.NotSame(resize.ToLayoutHint(), resize.ToLayoutHint());
    }

    /// <summary>
    /// <b>Decision revised 2026-08-05</b> (see the class doc comment): the four fields are now
    /// <see cref="LayoutLength"/>, so <c>generated/DDX/*.json</c> moves from <c>"Left": 315</c> to
    /// <c>"Left": "315px"</c> for every emitted entry. This replaces the old
    /// <c>SerializedShape_IsStillTheFourInts_WithNoLayoutHintField</c> pin, which asserted the
    /// opposite — that decision was explicit and is recorded here, not silently dropped.
    /// </summary>
    [Fact]
    public void SerializedShape_IsNowFourLengthStringsWithUnits() {
        var resize = new ResizeDialogAction {
            Left = LayoutLength.Px(315f), Top = LayoutLength.Px(738f),
            Width = LayoutLength.Px(1129f), Height = LayoutLength.Px(402f)
        };

        string json = JsonSerializer.Serialize(resize);

        Assert.Equal("{\"Left\":\"315px\",\"Top\":\"738px\",\"Width\":\"1129px\",\"Height\":\"402px\"}", json);
    }

    /// <summary>
    /// The consistency the decision reversal was made for: a percent-valued resize round-trips
    /// through System.Text.Json (the extractor's own serializer) preserving the unit rather than
    /// being coerced to px or losing the '%'. Distinctive, asymmetric fixture values so a
    /// round-trip that silently swapped a field or a unit would be caught.
    ///
    /// <para>The Newtonsoft-side counterpart of this round trip — the serializer the Unity
    /// mod-override path actually reads with — lives in
    /// <c>UnityProject/Assets/Tests/Editor/UI/LayoutNewtonsoftOverrideTests.cs</c> and is deferred
    /// until the Unity Editor host is back (see the DLL-pair rebuild note in the task brief).</para>
    /// </summary>
    [Fact]
    public void PercentValuedAction_RoundTripsThroughSystemTextJson_PreservingTheUnit() {
        var original = new ResizeDialogAction {
            Left = LayoutLength.Percent(12.5f), Top = LayoutLength.Px(66f),
            Width = LayoutLength.Percent(91.75f), Height = LayoutLength.Percent(48f)
        };

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ResizeDialogAction>(json)!;

        Assert.Equal(LayoutLength.Percent(12.5f), restored.Left);
        Assert.Equal(LayoutLength.Px(66f), restored.Top);
        Assert.Equal(LayoutLength.Percent(91.75f), restored.Width);
        Assert.Equal(LayoutLength.Percent(48f), restored.Height);
    }
}
