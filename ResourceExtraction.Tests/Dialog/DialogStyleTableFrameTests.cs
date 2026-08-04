namespace ResourceExtraction.Tests.Dialog;

using System.IO;
using System.Text.Json;
using GameData.Resources.Dialog;
using GameData.Resources.Layout;
using GameData.Resources.Menu;
using ResourceExtraction.Imaging;
using ResourceExtraction.Providers;
using ResourceExtractor.Extensions;
using Xunit;

/// <summary>
/// The dialog surface's <see cref="DesignFrame"/> — the coordinate space
/// <see cref="DialogStyle.DefaultArea"/> and every <see cref="DialogLayout"/> value resolve
/// against, and the only place <see cref="LayoutFit"/> can be stated for a dialog.
///
/// <para>Before this the dialog path passed <c>null</c> to <c>CanonicalStage.GetOrCreate</c> and
/// got the right box only because the null fallback happened to be the canonical frame. That is
/// the failure mode these tests are built against: every dimension assertion below is written as
/// the VGA mode size times the aspect factor, computed in the test body, so a frame that came
/// from a hardcoded 1600x1200 somewhere would still have to agree with the factors — and the
/// serialization fixtures use <b>800x600 / Fill</b>, which nothing in the shipped data or in any
/// fallback can produce by accident.</para>
/// </summary>
public class DialogStyleTableFrameTests {
    /// <summary>Reader options matching what <c>ResourceExtensions.ToJson</c> writes with: the
    /// emitted document spells <see cref="LayoutFit"/> as a NAME.</summary>
    private static readonly JsonSerializerOptions ReadOptions = new() {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>Walk up from the test output dir to find OriginalGame/&lt;name&gt; (present on dev
    /// machines, absent on CI). Returns null when the shipped data isn't available.</summary>
    private static string? FindGameDir(string name) {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return Path.GetDirectoryName(candidate);
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    /// <summary>
    /// The stamp itself. Stated as mode size x aspect factor rather than as 1600x1200: a slip that
    /// wrote the canonical numbers as literals somewhere would still have to reconcile with
    /// <see cref="AspectCorrection"/>'s factors to pass, and if a factor ever changes this test
    /// changes with it instead of pinning a stale number.
    /// </summary>
    [Fact]
    public void ApplyStampsTheCanonicalSpace_DerivedFromTheVgaModeAndTheAspectFactors() {
        var table = new DialogStyleTable();

        CanonicalSpace.Apply(table);

        Assert.Equal(AspectCorrection.VgaWidth * AspectCorrection.VgaScaleX, table.Frame.Width);
        Assert.Equal(AspectCorrection.VgaHeight * AspectCorrection.VgaScaleY, table.Frame.Height);
        // Contain is the faithful default: the original ran at a fixed 4:3, so the shipped dialog
        // must pillarbox exactly as it always has. Fill is what an override reaches for.
        Assert.Equal(LayoutFit.Contain, table.Frame.Fit);
    }

    /// <summary>
    /// FAITHFULNESS. The dialog surface must land in the same space as every other screen, which
    /// is what makes "nothing observable changed" true: before this task the dialog stage was
    /// built from <c>CanonicalStage</c>'s null fallback, whose dimensions are the canonical ones.
    /// Comparing against the frame a REQ screen gets — through a completely different code path —
    /// is the assertion that would catch a dialog frame drifting away from the rest of the UI.
    /// </summary>
    [Fact]
    public void TheDialogFrame_IsTheSameSpaceEveryOtherScreenResourceIsStampedWith() {
        var table = new DialogStyleTable();
        var screen = new UserInterface("REQ_TEST.DAT");

        CanonicalSpace.Apply(table);
        CanonicalSpace.Apply(screen);

        Assert.Equal(screen.Frame.Width, table.Frame.Width);
        Assert.Equal(screen.Frame.Height, table.Frame.Height);
        Assert.Equal(screen.Frame.Fit, table.Frame.Fit);
    }

    /// <summary>
    /// The contract that makes the stamp mandatory rather than decorative: GameData has no
    /// canonical dimensions of its own (they are derived from <see cref="AspectCorrection"/>,
    /// which lives on the extraction side), so a table nobody stamped carries a 0x0 frame. Pinned
    /// as a test so a future reader who wonders why <c>CreateShipped</c> doesn't just set it finds
    /// the answer executable rather than only in a comment.
    /// </summary>
    [Fact]
    public void AnUnstampedTable_HasACollapsedFrame_WhichIsWhyEveryShippedPathMustStampIt() {
        var table = DialogStyleTable.CreateShipped();

        Assert.Equal(0, table.Frame.Width);
        Assert.Equal(0, table.Frame.Height);
    }

    /// <summary>
    /// The provider touchpoint — the path <c>BakResourceProvider</c> takes in Unity when nothing
    /// is overridden. Needs the game archive only because <c>GeneralResourceProvider</c>'s
    /// constructor reads KRONDOR.001's directory; the table itself comes from code.
    /// </summary>
    [SkippableFact]
    public void GeneralResourceProvider_StampsTheFrame_OnTheTableItHandsOut() {
        string? gameDir = FindGameDir("KRONDOR.001");
        Skip.If(gameDir == null, "OriginalGame/KRONDOR.001 not found");

        var provider = new GeneralResourceProvider(gameDir!);

        DialogStyleTable table = provider.GetResource<DialogStyleTable>(DialogStyleTable.ResourceId);

        Assert.Equal(AspectCorrection.VgaWidth * AspectCorrection.VgaScaleX, table.Frame.Width);
        Assert.Equal(AspectCorrection.VgaHeight * AspectCorrection.VgaScaleY, table.Frame.Height);
        Assert.Equal(LayoutFit.Contain, table.Frame.Fit);
    }

    /// <summary>
    /// The emitted document carries the frame, and reads back as authored. The fixture is
    /// <b>800x600 / Fill</b> deliberately: both dimensions and the fit differ from anything the
    /// shipped data, the type defaults or any fallback could produce, so a serializer that dropped
    /// the property (or an enum written as an int) cannot pass by coincidence.
    /// </summary>
    [Fact]
    public void TheFrame_RoundTripsThroughTheEmittedDocument_WithItsFitAsAName() {
        var table = new DialogStyleTable { Frame = new DesignFrame { Width = 800, Height = 600, Fit = LayoutFit.Fill } };

        string json = table.ToJson();

        Assert.Contains("\"Fit\": \"Fill\"", json);
        Assert.DoesNotContain("\"Fit\": 1", json);

        DialogStyleTable restored = JsonSerializer.Deserialize<DialogStyleTable>(json, ReadOptions)!;

        Assert.Equal(800, restored.Frame.Width);
        Assert.Equal(600, restored.Frame.Height);
        Assert.Equal(LayoutFit.Fill, restored.Frame.Fit);
    }

    /// <summary>
    /// The shipped document an author copies from states the frame, so <see cref="LayoutFit"/> is
    /// discoverable without reading the source. Asserted on the real emitted JSON rather than on
    /// the object, because the emitted file is what a mod author actually opens.
    /// </summary>
    [Fact]
    public void TheEmittedShippedDocument_StatesTheCanonicalFrameAndItsFit() {
        var table = new DialogStyleTable();
        CanonicalSpace.Apply(table);

        string json = table.ToJson();

        Assert.Contains("\"Width\": " + (AspectCorrection.VgaWidth * AspectCorrection.VgaScaleX), json);
        Assert.Contains("\"Height\": " + (AspectCorrection.VgaHeight * AspectCorrection.VgaScaleY), json);
        Assert.Contains("\"Fit\": \"Contain\"", json);
    }
}
