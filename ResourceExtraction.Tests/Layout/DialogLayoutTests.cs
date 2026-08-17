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

    /// <summary>
    /// <b>The narrative body top is deliberately NOT here.</b> It used to assert
    /// <c>ScaleVgaY(30)</c> = 180 as "the original's vertical inset", and that was wrong: the
    /// original reads the body's top inset off the STYLE ROW (<c>y += field_7</c> at 0x49050, wrap
    /// height <c>-= field_7 + field_8</c> at 0x4906e), and no row carries 30 — the strips carry 5,
    /// the bordered boxes 3, the full-screen row 1. The real numbers are asserted against the table
    /// in <c>BetrayalAtKrondor.Tests.Dialog.DialogStyleTableInsetTests</c>.
    ///
    /// <para><see cref="DialogLayout.NarrativeBodyTop"/> now defaults to
    /// <see cref="LayoutLength.Auto"/> — "not overridden" — which is what
    /// <see cref="NarrativeBodyTop_DefaultsToAuto_SoTheStyleRowDecides"/> pins.</para>
    /// </summary>
    [Fact]
    public void BodyTextOffsets_AreTheOriginalsVerticalInsets() {
        Assert.Equal(LayoutLength.Px(AspectCorrection.ScaleVgaY(6)), _layout.SpeakerTop);
        // A plain float, not a LayoutLength — see SpeakerToBodyGap's remarks: it is only ever a
        // term in a px sum, so a percentage there could never resolve.
        Assert.Equal((float)AspectCorrection.ScaleVgaY(20), _layout.SpeakerToBodyGap);
    }

    /// <summary>
    /// Auto is the shipped value, and it means "the style row decides". A concrete default here
    /// cannot be faithful: one number would have to serve rows whose insets differ 5 : 3 : 1.
    /// </summary>
    [Fact]
    public void NarrativeBodyTop_DefaultsToAuto_SoTheStyleRowDecides() =>
        Assert.Equal(LayoutLength.Auto, _layout.NarrativeBodyTop);

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

        // The four assertions above are only a fence on the asymmetry while the two factors
        // genuinely differ — if AspectCorrection's horizontal factor were ever changed to match
        // the vertical one, they would all still pass while saying nothing. So the premise gets
        // its own assertion, which can fail on its own. (Restating it per-value as
        // `NotEqual(ScaleVgaX(1), ChromeBorderWidth)` could not: with `== ScaleVgaY(1)` already
        // asserted, 5 != 6 makes it true by arithmetic, not by anything about the layout.)
        Assert.NotEqual(AspectCorrection.ScaleVgaX(1), AspectCorrection.ScaleVgaY(1));
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
        // Lengths travel as unit-bearing strings, not {Value,Unit} objects. Asserted on a
        // property that carries a NUMBER — NarrativeBodyTop is Auto now, and "auto" would prove
        // only that a string came out, not that the value and its unit survived together.
        Assert.Contains("\"SpeakerTop\": \"36px\"", json);
        Assert.DoesNotContain("\"SpeakerTop\": 36", json);
        // Auto is a real shipped value and must survive as itself: an author's override document
        // omitting the inset has to keep meaning "the style row decides", not "zero".
        Assert.Contains("\"NarrativeBodyTop\": \"auto\"", json);
        // ...and the plain-float scalars travel as bare numbers, so no unit an author writes
        // there can look like it survived. (SpeakerToBodyGap used to be a LayoutLength.)
        Assert.Contains("\"SpeakerToBodyGap\": 120", json);
        Assert.DoesNotContain("\"SpeakerToBodyGap\": \"120px\"", json);

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
    ///
    /// <para><b>Scope, deliberately narrow.</b> Only values the RENDERER can actually resolve as
    /// a percentage appear here. <c>SpeakerToBodyGap</c> used to, and that was a lie in test
    /// form: the unit really did round-trip at this layer, while <c>ResolveBodyTop</c> refused
    /// the whole sum and dropped the body back to <c>NarrativeBodyTop</c> — an author following
    /// this test would have moved every speaker'd body to 180px. It is a plain <c>float</c> now,
    /// so the authoring form no longer exists to be endorsed.</para>
    /// </summary>
    [Fact]
    public void AnOverrideCanRestateTheResolvableOffsetsInPercent_AndTheUnitSurvives() {
        const string json =
            "{\"Layout\":{\"NarrativeBodyTop\":\"17.3%\",\"SpeakerTop\":\"4.1%\","
            + "\"SpeakerPill\":{\"Padding\":{\"Left\":\"6.25%\"}}}}";

        DialogLayout layout = JsonSerializer.Deserialize<DialogStyleTable>(json,
            new JsonSerializerOptions {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!.Layout;

        Assert.Equal(LayoutLength.Percent(17.3f), layout.NarrativeBodyTop);
        Assert.Equal(LayoutLengthUnit.Percent, layout.NarrativeBodyTop.Unit);
        // SpeakerTop resolves ON ITS OWN — it is the speaker label's own top inset — which is
        // exactly why it stays a LayoutLength while SpeakerToBodyGap does not.
        Assert.Equal(LayoutLength.Percent(4.1f), layout.SpeakerTop);
        Assert.Equal(LayoutLength.Percent(6.25f), layout.SpeakerPill.Padding!.Left);
        Assert.Equal(LayoutLengthUnit.Percent, layout.SpeakerPill.Padding.Left.Unit);
    }

    /// <summary>
    /// THE READ-DIRECTION FENCE. Every other test on this type compares a
    /// <c>new DialogStyleTable()</c> against a <c>new DialogLayout()</c> — defaults against
    /// defaults — so each of them still passes if the property is dropped from serialization
    /// entirely. Make <c>ChromeShadowOffset</c> get-only and it still EMITS (so
    /// <c>make verify-generated</c> stays clean and the corpus still matches), no test goes red,
    /// and overriding it from <c>DAT/DIALSTYL.json</c> silently stops working.
    ///
    /// <para>So this one states a whole layout in values that are nothing like the shipped
    /// numbers — asymmetric, non-round, and no two sharing a value, so a property that lost its
    /// setter, got wired to a sibling, or fell back to its default lands on a number this test
    /// names. These eleven are the full set: they are what a mod author is being sold.</para>
    /// </summary>
    [Fact]
    public void EveryLayoutValue_SurvivesAnOverrideDocument_NotJustTheOnesWithADefault() {
        const string json =
            "{\"Layout\":{"
            + "\"SpeakerPillRow\":{\"Left\":\"11px\",\"Top\":\"37px\",\"Right\":\"13px\"},"
            + "\"SpeakerPill\":{\"Padding\":{"
            + "\"Left\":\"113px\",\"Top\":\"29px\",\"Right\":\"71px\",\"Bottom\":\"43px\"}},"
            + "\"SpeakerPillShadowOffset\":17,\"SpeakerPillBorderWidth\":23,"
            + "\"NarrativeBodyTop\":\"211px\",\"SpeakerTop\":\"53px\",\"SpeakerToBodyGap\":137,"
            + "\"ChromeBorderWidth\":31,\"ChromeShadowOffset\":19,"
            + "\"TextShadowOffsetX\":7,\"TextShadowOffsetY\":11}}";

        DialogLayout layout = JsonSerializer.Deserialize<DialogStyleTable>(json,
            new JsonSerializerOptions {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!.Layout;

        // The pill row's Left/Right are not 0 here, so an implementation that hardcoded the
        // "span the panel" 0s cannot satisfy them.
        Assert.Equal(LayoutLength.Px(11f), layout.SpeakerPillRow.Left);
        Assert.Equal(LayoutLength.Px(37f), layout.SpeakerPillRow.Top);
        Assert.Equal(LayoutLength.Px(13f), layout.SpeakerPillRow.Right);

        // Four sides, four different numbers: "one horizontal + one vertical" cannot produce them.
        LayoutPadding padding = layout.SpeakerPill.Padding!;
        Assert.Equal(LayoutLength.Px(113f), padding.Left);
        Assert.Equal(LayoutLength.Px(29f), padding.Top);
        Assert.Equal(LayoutLength.Px(71f), padding.Right);
        Assert.Equal(LayoutLength.Px(43f), padding.Bottom);

        Assert.Equal(17f, layout.SpeakerPillShadowOffset);
        Assert.Equal(23f, layout.SpeakerPillBorderWidth);
        Assert.Equal(LayoutLength.Px(211f), layout.NarrativeBodyTop);
        Assert.Equal(LayoutLength.Px(53f), layout.SpeakerTop);
        Assert.Equal(137f, layout.SpeakerToBodyGap);
        Assert.Equal(31f, layout.ChromeBorderWidth);
        Assert.Equal(19f, layout.ChromeShadowOffset);
        Assert.Equal(7f, layout.TextShadowOffsetX);
        Assert.Equal(11f, layout.TextShadowOffsetY);

        // And none of it is a shipped default that happened to match: every value above differs
        // from the one a fresh table carries, so "the override was ignored" cannot pass.
        var shipped = new DialogLayout();
        Assert.NotEqual(shipped.SpeakerPillShadowOffset, layout.SpeakerPillShadowOffset);
        Assert.NotEqual(shipped.SpeakerPillBorderWidth, layout.SpeakerPillBorderWidth);
        Assert.NotEqual(shipped.SpeakerToBodyGap, layout.SpeakerToBodyGap);
        Assert.NotEqual(shipped.ChromeBorderWidth, layout.ChromeBorderWidth);
        Assert.NotEqual(shipped.ChromeShadowOffset, layout.ChromeShadowOffset);
        Assert.NotEqual(shipped.TextShadowOffsetX, layout.TextShadowOffsetX);
        Assert.NotEqual(shipped.TextShadowOffsetY, layout.TextShadowOffsetY);
        Assert.NotEqual(shipped.SpeakerPillRow.Left, layout.SpeakerPillRow.Left);
        Assert.NotEqual(shipped.SpeakerPillRow.Right, layout.SpeakerPillRow.Right);
        Assert.NotEqual(shipped.SpeakerPill.Padding!.Top, padding.Top);
        Assert.NotEqual(shipped.SpeakerPill.Padding.Right, padding.Right);
        Assert.NotEqual(shipped.SpeakerPill.Padding.Bottom, padding.Bottom);
    }

    /// <summary>
    /// The other half of the read direction: a distinctive layout must also make the ROUND TRIP
    /// — serialize, reload, same values. <see cref="Layout_RoundTripsThroughTheExtractorsSerializer_KeepingItsUnits"/>
    /// round-trips the DEFAULTS, so a property that stopped being written would come back as its
    /// default and that test could not tell.
    /// </summary>
    [Fact]
    public void ADistinctiveLayout_RoundTripsThroughTheExtractorsSerializer() {
        var table = new DialogStyleTable {
            Layout = new DialogLayout {
                SpeakerPillRow = new LayoutHint {
                    Left = LayoutLength.Px(11f),
                    Top = LayoutLength.Px(37f),
                    Right = LayoutLength.Px(13f),
                },
                SpeakerPill = new LayoutHint {
                    Position = LayoutPosition.InFlow,
                    Padding = new LayoutPadding {
                        Left = LayoutLength.Px(113f),
                        Top = LayoutLength.Px(29f),
                        Right = LayoutLength.Px(71f),
                        Bottom = LayoutLength.Px(43f),
                    },
                },
                SpeakerPillShadowOffset = 17f,
                SpeakerPillBorderWidth = 23f,
                NarrativeBodyTop = LayoutLength.Px(211f),
                SpeakerTop = LayoutLength.Px(53f),
                SpeakerToBodyGap = 137f,
                ChromeBorderWidth = 31f,
                ChromeShadowOffset = 19f,
                TextShadowOffsetX = 7f,
                TextShadowOffsetY = 11f,
            }
        };

        DialogLayout restored = JsonSerializer.Deserialize<DialogStyleTable>(table.ToJson(),
            new JsonSerializerOptions {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!.Layout;

        Assert.Equal(LayoutLength.Px(11f), restored.SpeakerPillRow.Left);
        Assert.Equal(LayoutLength.Px(37f), restored.SpeakerPillRow.Top);
        Assert.Equal(LayoutLength.Px(13f), restored.SpeakerPillRow.Right);
        Assert.Equal(LayoutLength.Px(113f), restored.SpeakerPill.Padding!.Left);
        Assert.Equal(LayoutLength.Px(29f), restored.SpeakerPill.Padding.Top);
        Assert.Equal(LayoutLength.Px(71f), restored.SpeakerPill.Padding.Right);
        Assert.Equal(LayoutLength.Px(43f), restored.SpeakerPill.Padding.Bottom);
        Assert.Equal(17f, restored.SpeakerPillShadowOffset);
        Assert.Equal(23f, restored.SpeakerPillBorderWidth);
        Assert.Equal(LayoutLength.Px(211f), restored.NarrativeBodyTop);
        Assert.Equal(LayoutLength.Px(53f), restored.SpeakerTop);
        Assert.Equal(137f, restored.SpeakerToBodyGap);
        Assert.Equal(31f, restored.ChromeBorderWidth);
        Assert.Equal(19f, restored.ChromeShadowOffset);
        Assert.Equal(7f, restored.TextShadowOffsetX);
        Assert.Equal(11f, restored.TextShadowOffsetY);
    }
}
