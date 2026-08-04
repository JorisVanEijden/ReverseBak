namespace ResourceExtraction.Tests.Layout;

using System.Text.Json;

using GameData.Resources.Dialog;
using GameData.Resources.Layout;

using ResourceExtraction.Imaging;

using ResourceExtractor.Extensions;

using Xunit;

/// <summary>
/// Faithfulness gate for the dialog panel's INTERNAL geometry: these are exactly the eleven
/// constants (plus one bare literal) <c>DialogPanelBuilder.cs</c> carried before the conversion.
/// If one of these changes, a shipped dialog has moved relative to the original.
///
/// <para>Every expectation is stated as the RAW VGA number multiplied by
/// <see cref="AspectCorrection.ScaleVgaX"/>/<see cref="AspectCorrection.ScaleVgaY"/> in the test
/// body — never as the canonical number — so the x5/x6 mapping is what gets proven rather than a
/// literal copied identically into both places. Length assertions carry their unit
/// (<see cref="LayoutLength"/> compares Value AND Unit), because "right number, wrong unit" is a
/// defect class this project has already shipped.</para>
/// </summary>
public class DialogLayoutTests {
    private readonly DialogLayout _layout = new();

    [Fact]
    public void SpeakerPillRow_IsTheFullWidthCentringRow_InsetOneOriginalPixelFromTheAreaTop() {
        // Top inset VGA 1 -> x6. Left/Right 0 span the panel so the flow has something to centre
        // in; drop either and the pill drifts to wherever its content starts.
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(1)), _layout.SpeakerPillRow.Top);
        Assert.Equal(LayoutLength.Px(0f), _layout.SpeakerPillRow.Left);
        Assert.Equal(LayoutLength.Px(0f), _layout.SpeakerPillRow.Right);
        Assert.Equal(LayoutPosition.Absolute, _layout.SpeakerPillRow.Position);
        Assert.Equal(LayoutAnchor.TopLeft, _layout.SpeakerPillRow.Anchor);

        Assert.NotNull(_layout.SpeakerPillRow.Flow);
        Assert.Equal(LayoutFlowDirection.Row, _layout.SpeakerPillRow.Flow!.Direction);
        Assert.Equal(LayoutFlowJustify.Center, _layout.SpeakerPillRow.Flow.Justify);
        Assert.Equal(LayoutFlowAlign.Center, _layout.SpeakerPillRow.Flow.Align);
        Assert.False(_layout.SpeakerPillRow.Flow.Wrap);

        // The row places the pill; it must not also try to place itself in a parent's flow.
        Assert.Null(_layout.SpeakerPillRow.Padding);
    }

    [Fact]
    public void SpeakerPill_IsAnInFlowBoxWithTheOriginalsGenerousPadding() {
        Assert.Equal(LayoutPosition.InFlow, _layout.SpeakerPill.Position);

        Assert.NotNull(_layout.SpeakerPill.Padding);
        LayoutPadding padding = _layout.SpeakerPill.Padding!;
        // Horizontal padding takes the HORIZONTAL factor (VGA 18 x5), vertical the vertical one
        // (VGA 3 x6) — an axis slip here is exactly what a single-factor implementation produces.
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaX(18)), padding.Left);
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaX(18)), padding.Right);
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(3)), padding.Top);
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(3)), padding.Bottom);

        Assert.NotNull(_layout.SpeakerPill.Flow);
        Assert.Equal(LayoutFlowDirection.Row, _layout.SpeakerPill.Flow!.Direction);
        Assert.Equal(LayoutFlowJustify.Center, _layout.SpeakerPill.Flow.Justify);
        Assert.Equal(LayoutFlowAlign.Center, _layout.SpeakerPill.Flow.Align);

        // An in-flow element placed by its parent must carry no insets of its own, or
        // LayoutApplier warns and the authoring intent is contradictory.
        Assert.Equal(LayoutLength.Auto, _layout.SpeakerPill.Left);
        Assert.Equal(LayoutLength.Auto, _layout.SpeakerPill.Top);
    }

    [Fact]
    public void BodyTextOffsets_AreTheOriginalsVerticalInsets() {
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(30)), _layout.NarrativeBodyTop);
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(6)), _layout.SpeakerTop);
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(20)), _layout.SpeakerToBodyGap);
    }

    /// <summary>
    /// The three single-scalar rims all carry the VERTICAL factor on BOTH axes. That is what
    /// ships — the pre-conversion constants were <c>1f * Canonical.VgaScaleY</c> and the pill's
    /// border was a bare <c>6</c> — and it is asserted here so that "correcting" any of them to a
    /// per-axis 5/6 pair, which would move shipped pixels, goes red instead of shipping.
    /// </summary>
    [Fact]
    public void EdgeWidthsAndPanelShadows_CarryTheVerticalFactorOnBothAxes() {
        Assert.Equal((float)AspectCorrection.ScaleVgaY(1), _layout.ChromeBorderWidth);
        Assert.Equal((float)AspectCorrection.ScaleVgaY(1), _layout.ChromeShadowOffset);
        Assert.Equal((float)AspectCorrection.ScaleVgaY(1), _layout.SpeakerPillShadowOffset);
        Assert.Equal((float)AspectCorrection.ScaleVgaY(1), _layout.SpeakerPillBorderWidth);

        // Not the horizontal factor: this is the assertion that fails if someone "fixes" the
        // asymmetry. AspectCorrection's two factors genuinely differ, so this is a real check.
        Assert.NotEqual((float)AspectCorrection.ScaleVgaX(1), _layout.ChromeBorderWidth);
    }

    /// <summary>
    /// The text shadow IS per-axis — the one place the original's non-square pixels survive as
    /// two different numbers, exactly as <c>InventoryLayout.TextShadowOffsetX/Y</c> does.
    /// </summary>
    [Fact]
    public void TextShadowOffset_IsPerAxis_UnlikeTheRims() {
        Assert.Equal((float)AspectCorrection.ScaleVgaX(1), _layout.TextShadowOffsetX);
        Assert.Equal((float)AspectCorrection.ScaleVgaY(1), _layout.TextShadowOffsetY);
        Assert.NotEqual(_layout.TextShadowOffsetX, _layout.TextShadowOffsetY);
    }

    [Fact]
    public void DialogStyleTable_HasALayoutByDefault_SoAConsumerWithNoOverrideStillHasGeometry() {
        Assert.NotNull(new DialogStyleTable().Layout);
        Assert.NotNull(DialogStyleTable.CreateShipped().Layout);
    }

    /// <summary>
    /// Two tables must not share a layout instance, for the same reason they must not share rows:
    /// the shipped table and a modded one coexist, and mutating one through the other would
    /// silently restyle every later dialog.
    /// </summary>
    [Fact]
    public void TwoTables_DoNotShareALayoutInstance() {
        Assert.NotSame(new DialogStyleTable().Layout, new DialogStyleTable().Layout);
    }

    /// <summary>
    /// The layout must survive the extractor's own serializer, units intact — the emitted
    /// <c>DIALSTYL.json</c> is both the extractor's deliverable and the document a mod author
    /// copies and edits.
    /// </summary>
    [Fact]
    public void Layout_RoundTripsThroughTheExtractorsSerializer_KeepingItsUnits() {
        string json = new DialogStyleTable().ToJson();

        Assert.Contains("\"Layout\"", json);
        // Lengths travel as unit-bearing strings, not {Value,Unit} objects.
        Assert.Contains("\"SpeakerToBodyGap\": \"120px\"", json);
        Assert.DoesNotContain("\"SpeakerToBodyGap\": 120", json);

        var readOptions = new JsonSerializerOptions {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        DialogLayout restored = JsonSerializer.Deserialize<DialogStyleTable>(json, readOptions)!.Layout;

        Assert.Equal(_layout.NarrativeBodyTop, restored.NarrativeBodyTop);
        Assert.Equal(_layout.SpeakerTop, restored.SpeakerTop);
        Assert.Equal(_layout.SpeakerToBodyGap, restored.SpeakerToBodyGap);
        Assert.Equal(_layout.SpeakerPillRow.Top, restored.SpeakerPillRow.Top);
        Assert.Equal(_layout.SpeakerPill.Padding!.Left, restored.SpeakerPill.Padding!.Left);
        Assert.Equal(_layout.SpeakerPill.Padding.Top, restored.SpeakerPill.Padding.Top);
        Assert.Equal(LayoutPosition.InFlow, restored.SpeakerPill.Position);
        Assert.Equal(_layout.ChromeBorderWidth, restored.ChromeBorderWidth);
        Assert.Equal(_layout.ChromeShadowOffset, restored.ChromeShadowOffset);
        Assert.Equal(_layout.SpeakerPillShadowOffset, restored.SpeakerPillShadowOffset);
        Assert.Equal(_layout.SpeakerPillBorderWidth, restored.SpeakerPillBorderWidth);
        Assert.Equal(_layout.TextShadowOffsetX, restored.TextShadowOffsetX);
        Assert.Equal(_layout.TextShadowOffsetY, restored.TextShadowOffsetY);
    }

    /// <summary>
    /// An author restating one of these in PERCENT must arrive as a percentage, not as a bare
    /// number that silently means px. Fixture values are asymmetric and non-round so no
    /// defaulting or unit-blind path can reproduce them by accident.
    /// </summary>
    [Fact]
    public void AnOverrideCanRestateTheOffsetsInPercent_AndTheUnitSurvives() {
        const string json =
            "{\"Layout\":{\"NarrativeBodyTop\":\"17.3%\",\"SpeakerTop\":\"4.1%\","
            + "\"SpeakerToBodyGap\":\"9.7%\",\"SpeakerPill\":{\"Padding\":{\"Left\":\"6.25%\"}}}}";

        DialogLayout layout = JsonSerializer.Deserialize<DialogStyleTable>(json,
            new JsonSerializerOptions {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!.Layout;

        Assert.Equal(LayoutLength.Percent(17.3f), layout.NarrativeBodyTop);
        Assert.Equal(LayoutLengthUnit.Percent, layout.NarrativeBodyTop.Unit);
        Assert.Equal(LayoutLength.Percent(4.1f), layout.SpeakerTop);
        Assert.Equal(LayoutLength.Percent(9.7f), layout.SpeakerToBodyGap);
        Assert.Equal(LayoutLength.Percent(6.25f), layout.SpeakerPill.Padding!.Left);
        Assert.Equal(LayoutLengthUnit.Percent, layout.SpeakerPill.Padding.Left.Unit);
    }
}
