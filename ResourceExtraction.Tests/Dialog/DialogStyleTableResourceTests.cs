namespace ResourceExtraction.Tests.Dialog;

using System.IO;
using System.Text.Json;
using GameData.Resources;
using GameData.Resources.Dialog;
using GameData.Resources.Layout;
using ResourceExtraction.Providers;
using ResourceExtractor.Extensions;
using Xunit;

/// <summary>
/// The dialog style table's <b>resource identity</b> — the thing that lets a mod author move or
/// resize a dialog box at all. Before this, <c>DialogStyleTable</c> was a static array compiled
/// into GameData.dll with no override path; now it is a synthesized <see cref="IResource"/> under
/// <c>DIALSTYL.DAT</c>, exactly as <c>ChapterCatalog</c> is under <c>CHAPTERS.DAT</c>, which is
/// what puts it on <c>OverrideResourceLocator</c>'s
/// <c>&lt;OverridePath&gt;/DAT/DIALSTYL.json</c> path.
///
/// <para>Fixture values throughout are asymmetric and non-round, and every length assertion
/// carries its unit: a bare number assertion passes just as happily against the wrong unit, and
/// "right number, wrong unit" is a defect class this project has already shipped.</para>
/// </summary>
public class DialogStyleTableResourceTests {
    /// <summary>
    /// Reader options matching what <c>ResourceExtensions.ToJson</c> writes with. The emitted
    /// document spells enums as NAMES (that is the point — see
    /// <see cref="ToJson_EncodesEveryEnumAsAString_NotAnInt"/>), so a reader without
    /// <c>JsonStringEnumConverter</c> cannot read it back. Newtonsoft, which is what actually
    /// reads an override document in Unity, handles enum names natively and needs no equivalent.
    /// </summary>
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

    [Fact]
    public void ResourceIdentity_IsTheSynthesizedDatResourceTheOverrideLocatorCanReach() {
        var table = new DialogStyleTable();

        // The extension is what OverrideResourceLocator keys on — it maps "DIALSTYL.DAT" to
        // <OverridePath>/DAT/DIALSTYL.json and refuses any key without one.
        Assert.Equal("DIALSTYL.DAT", DialogStyleTable.ResourceId);
        Assert.Equal(".DAT", Path.GetExtension(DialogStyleTable.ResourceId));
        Assert.Equal(ResourceType.DAT, table.Type);
        Assert.Equal(DialogStyleTable.ResourceId, table.Id);
    }

    [Fact]
    public void CreateShipped_CarriesTheGivenIdAndTheShippedRows() {
        DialogStyleTable table = DialogStyleTable.CreateShipped("SOMEOTHER.DAT");

        Assert.Equal("SOMEOTHER.DAT", table.Id);
        Assert.Equal(DialogStyleTable.Length, table.Rows.Length);
        // Row 2's shipped height is VGA 101 x6.
        Assert.Equal(LayoutLength.Px(606f), table.Get(2).DefaultArea.Height);
    }

    /// <summary>
    /// The provider touchpoint: <c>CanProvideResource</c>/<c>GetResource</c> must answer for a
    /// resource that has no archive member at all, the same way they already do for CHAPTERS.DAT.
    /// Needs the game archive only because <c>GeneralResourceProvider</c>'s constructor reads
    /// KRONDOR.001's directory — the table itself comes from code.
    /// </summary>
    [SkippableFact]
    public void GeneralResourceProvider_ProvidesTheTable_ThoughItIsNotAnArchiveMember() {
        string? gameDir = FindGameDir("KRONDOR.001");
        Skip.If(gameDir == null, "OriginalGame/KRONDOR.001 not found");

        var provider = new GeneralResourceProvider(gameDir!);

        Assert.True(provider.CanProvideResource(DialogStyleTable.ResourceId));
        // Case-insensitively, like the ChapterCatalog check next to it.
        Assert.True(provider.CanProvideResource("dialstyl.dat"));

        DialogStyleTable table = provider.GetResource<DialogStyleTable>(DialogStyleTable.ResourceId);

        Assert.Equal(DialogStyleTable.ResourceId, table.Id);
        Assert.Equal(ResourceType.DAT, table.Type);
        Assert.Equal(LayoutLength.Px(1470f), table.Get(2).DefaultArea.Width);
        Assert.Equal(LayoutLength.Px(726f), table.Get(5).DefaultArea.Height);
    }

    /// <summary>
    /// The emitted document is the extractor's deliverable AND the starting point a mod author
    /// copies, so its enums must read as names. Fifteen extractor call sites still serialize with
    /// a bare <c>JsonSerializerOptions</c> and emit <c>"Type": 5</c> (backlog task-52); this one
    /// goes through the shared options in <c>ResourceExtensions</c>, which carry
    /// <c>JsonStringEnumConverter</c>. Asserting the int form is ABSENT is the half that actually
    /// catches a regression — asserting only the string form would pass if both were somehow
    /// present, and asserting nothing is how the other fifteen got there.
    /// </summary>
    [Fact]
    public void ToJson_EncodesEveryEnumAsAString_NotAnInt() {
        string json = new DialogStyleTable().ToJson();

        Assert.Contains("\"Type\": \"DAT\"", json);
        Assert.DoesNotContain("\"Type\": 5", json);
        // LayoutHint's own enums travel in the same document and must read the same way.
        Assert.Contains("\"Position\": \"Absolute\"", json);
        Assert.Contains("\"Anchor\": \"TopLeft\"", json);
        Assert.DoesNotContain("\"Position\": 0", json);
        Assert.DoesNotContain("\"Anchor\": 0", json);
        // Lengths are bare strings with their unit attached, not {Value,Unit} objects.
        Assert.Contains("\"Left\": \"65px\"", json);
        Assert.Contains("\"Height\": \"606px\"", json);
    }

    /// <summary>
    /// Whole-table round trip through the serializer the extractor writes with: seven rows in,
    /// seven rows out, every pen and every unit intact.
    ///
    /// <para><b>This is NOT the guard on <see cref="DialogStyleTable.Rows"/> being an array.</b>
    /// It used to claim it was — "a pre-populated <c>List&lt;T&gt;</c> would be APPENDED to and
    /// come back with fourteen rows, and this is the assertion that would notice" — which is
    /// false about this test: it runs through System.Text.Json, which replaces a collection-valued
    /// property wholesale and never had the append behaviour, so changing <c>Rows</c> to a
    /// <c>List&lt;T&gt;</c> leaves it green while the Unity override path breaks. The append is
    /// NEWTONSOFT's, and the only test that exercises Newtonsoft against this type is
    /// <c>BakAgain.Tests.Editor.UI.DialogStyleTableOverrideTests.PartialOverrideOnDisk_ResolvedByTheRealLocator_MovesTheBoxAndKeepsItsChrome</c>
    /// ("the merge must not append rows"). Do not delete that assertion on the strength of this
    /// one.</para>
    /// </summary>
    [Fact]
    public void Table_RoundTripsThroughSystemTextJson_KeepingSevenRowsAndTheirUnits() {
        string json = new DialogStyleTable().ToJson();

        DialogStyleTable restored = JsonSerializer.Deserialize<DialogStyleTable>(json, ReadOptions)!;

        Assert.Equal(DialogStyleTable.Length, restored.Rows.Length);
        Assert.Null(restored.Rows[0]);
        Assert.False(restored.IsDefined(0));

        for (var row = 1; row < DialogStyleTable.Length; row++) {
            DialogStyle expected = new DialogStyleTable().Get(row);
            DialogStyle actual = restored.Get(row);

            Assert.Equal(expected.FillPenColor, actual.FillPenColor);
            Assert.Equal(expected.BorderPenColor, actual.BorderPenColor);
            Assert.Equal(expected.ShadowPenColor, actual.ShadowPenColor);
            Assert.Equal(expected.BodyTextPenColor, actual.BodyTextPenColor);
            Assert.Equal(expected.TextShadowPenSource, actual.TextShadowPenSource);
            Assert.Equal(expected.TextPadLeftPct, actual.TextPadLeftPct);
            Assert.Equal(expected.TextPadRightPct, actual.TextPadRightPct);
            // Unit-bearing: LayoutLength carries its unit, so these fail on "606%" as well as
            // on the wrong number.
            Assert.Equal(expected.DefaultArea.Left, actual.DefaultArea.Left);
            Assert.Equal(expected.DefaultArea.Top, actual.DefaultArea.Top);
            Assert.Equal(expected.DefaultArea.Width, actual.DefaultArea.Width);
            Assert.Equal(expected.DefaultArea.Height, actual.DefaultArea.Height);
            Assert.Equal(LayoutPosition.Absolute, actual.DefaultArea.Position);
            Assert.Equal(LayoutAnchor.TopLeft, actual.DefaultArea.Anchor);
        }
    }

    /// <summary>
    /// A whole-document deserialize REPLACES the rows array rather than being appended to the
    /// defaults: seven entries in, seven entries out, and the row the document states is the row
    /// that arrives.
    ///
    /// <para><b>Scoped to System.Text.Json, which is all this test can speak for.</b> STJ
    /// replaces a collection-valued property outright, so the seven here is not evidence about
    /// the array-vs-<c>List&lt;T&gt;</c> decision at all — see the note on
    /// <see cref="Table_RoundTripsThroughSystemTextJson_KeepingSevenRowsAndTheirUnits"/> for
    /// which test actually holds that line, and why deleting it would be a silent regression.</para>
    /// </summary>
    [Fact]
    public void WholeDocumentOverride_ReplacesTheRowsArray_RatherThanAppendingToTheDefaults() {
        const string json =
            "{\"Rows\":[null,null,null,null,null,null,"
            + "{\"FillPenColor\":3,\"DefaultArea\":{\"Left\":\"13.75%\",\"Top\":\"62.5%\","
            + "\"Width\":\"71.25%\",\"Height\":\"18.75%\"}}]}";

        DialogStyleTable table = JsonSerializer.Deserialize<DialogStyleTable>(json, ReadOptions)!;

        Assert.Equal(7, table.Rows.Length);
        Assert.Equal(LayoutLength.Percent(13.75f), table.Get(6).DefaultArea.Left);
        Assert.Equal(LayoutLength.Percent(62.5f), table.Get(6).DefaultArea.Top);
        Assert.Equal(LayoutLength.Percent(71.25f), table.Get(6).DefaultArea.Width);
        Assert.Equal(LayoutLength.Percent(18.75f), table.Get(6).DefaultArea.Height);
        Assert.Equal(3, table.Get(6).FillPenColor);
    }

    /// <summary>
    /// The trap, stated as a test rather than as a comment: a plain deserialize of a PARTIAL
    /// document — the normal thing a mod author writes — lands every unnamed field on
    /// <see cref="DialogStyle"/>'s own type defaults, which are all-zero pens, NOT the shipped
    /// row's. All-zero chrome makes the renderer skip the panel entirely: the author moves a box
    /// and loses the box.
    ///
    /// <para>This is why the Unity override path merges the document ONTO the shipped table at
    /// the JSON level (<c>OverrideResourceProvider</c> / <c>OverrideJsonMerge</c>) instead of
    /// deserializing it standalone — the JSON is the only place "omitted" and "explicitly 0" are
    /// still distinguishable. This test pins the raw behaviour so that if someone later removes
    /// the merge believing plain deserialization is enough, the reason it is not is written down
    /// and executable.</para>
    /// </summary>
    [Fact]
    public void PartialDocument_DeserializedStandalone_LosesTheShippedPens_WhichIsWhyTheMergeExists() {
        const string json = "{\"Rows\":[null,null,{\"DefaultArea\":{\"Left\":\"13.75%\"}}]}";

        DialogStyleTable table = JsonSerializer.Deserialize<DialogStyleTable>(json, ReadOptions)!;
        DialogStyle row2 = table.Get(2);

        // The move landed...
        Assert.Equal(LayoutLength.Percent(13.75f), row2.DefaultArea.Left);
        // ...and the box was lost with it. Shipped row 2 is Fill=1, Border=1, Bevel=4.
        Assert.Equal(0, row2.FillPenColor);
        Assert.Equal(0, row2.BorderPenColor);
        Assert.Equal(0, row2.ShadowPenColor);
        Assert.Equal(0f, row2.TextPadLeftPct);
        Assert.False(row2.UsesTexturedFill);
        Assert.False(row2.HasBorder);
        Assert.False(row2.HasDropShadow);
    }
}
