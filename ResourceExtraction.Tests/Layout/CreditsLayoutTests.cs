namespace ResourceExtraction.Tests.Layout;

using System.IO;
using System.Text;

using GameData.Resources.Credits;
using GameData.Resources.Layout;

using ResourceExtraction.Extractors;
using ResourceExtraction.Imaging;

using Xunit;

/// <summary>Faithfulness gate for the credits layout: these are exactly the constants
/// CreditsView.cs carried before the conversion (see the plan's table). If one of these
/// changes, the credits screen has moved relative to the original.</summary>
public class CreditsLayoutTests {
    [Fact]
    public void Defaults_MatchTheOriginalCanonicalGeometry() {
        var layout = new CreditsLayout();

        // Title: VGA y=41 -> 246 canonical is the title's TOP EDGE (title.style.top in the
        // pre-conversion view), not a band height — sizes to its own content, horizontally
        // centred.
        Assert.Equal(LayoutLength.Px(246f), layout.Title.Top);
        Assert.Equal(LayoutLength.Auto, layout.Title.Height);
        Assert.Equal(LayoutAnchor.TopCenter, layout.Title.Anchor);

        // Window: top VGA y=54 -> 324; bottom VGA y=158 -> 948, so height 948-324 = 624.
        Assert.Equal(LayoutLength.Px(324f), layout.Window.Top);
        Assert.Equal(LayoutLength.Px(624f), layout.Window.Height);

        // Row: left VGA x=42 -> 210; name right edge VGA x=277 -> 1385, so right inset
        // 1600-1385 = 215. Height VGA 11 -> 66. Top-aligned (Start), matching the original's
        // labels (neither sets a top offset); the leader's own 25%-from-bottom placement stays
        // a paint concern, not a flex alignment.
        Assert.Equal(LayoutLength.Px(210f), layout.Row.Left);
        Assert.Equal(LayoutLength.Px(215f), layout.Row.Right);
        Assert.Equal(LayoutLength.Px(66f), layout.Row.Height);
        Assert.NotNull(layout.Row.Flow);
        Assert.Equal(LayoutFlowDirection.Row, layout.Row.Flow.Direction);
        Assert.Equal(LayoutFlowJustify.SpaceBetween, layout.Row.Flow.Justify);
        Assert.Equal(LayoutFlowAlign.Start, layout.Row.Flow.Align);
        Assert.False(layout.Row.Flow.Wrap);

        // Paint parameters, unchanged from the coordinate model.
        Assert.Equal(LayoutLength.Px(48f), layout.FontSize);
        Assert.Equal(LayoutLength.Px(96f), layout.FadeTopBand);
        Assert.Equal(LayoutLength.Px(102f), layout.FadeBottomBand);
        Assert.Equal(LayoutLength.Px(20f), layout.LeaderDotPitch);
        Assert.Equal(LayoutLength.Px(2.5f), layout.LeaderDotRadius);
        Assert.Equal(LayoutLength.Px(10f), layout.LeaderGap);
    }

    [Fact]
    public void CreditsData_HasALayoutByDefault() {
        Assert.NotNull(new CreditsData("CRED.DAT").Layout);
    }

    [Fact]
    public void CredExtractor_EmitsTheFaithfulGeometry() {
        // Minimal well-formed CRED.DAT: a title plus one (role, name) pair.
        byte[] blob = Encoding.ASCII.GetBytes("CREDITS\0PROGRAMMING:\0Steve Cordon\0");
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true)) {
            writer.Write((ushort)3);      // count
            writer.Write((ushort)0);      // offsets[0] -> "CREDITS"
            writer.Write((ushort)8);      // offsets[1] -> "PROGRAMMING:"
            writer.Write((ushort)21);     // offsets[2] -> "Steve Cordon"
            writer.Write((ushort)blob.Length);
            writer.Write(blob);
        }

        stream.Position = 0;
        CreditsData credits = new CredExtractor().Extract("CRED.DAT", stream);

        Assert.Equal("CREDITS", credits.Title);
        Assert.NotNull(credits.Layout);

        // Title: VGA y=41 -> 246 canonical is the title's TOP EDGE, not a band height — sizes
        // to its own content, horizontally centred.
        Assert.Equal(LayoutLength.Px(246f), credits.Layout.Title.Top);
        Assert.Equal(LayoutLength.Auto, credits.Layout.Title.Height);
        Assert.Equal(LayoutAnchor.TopCenter, credits.Layout.Title.Anchor);

        // Window: top VGA y=54 -> 324; bottom VGA y=158 -> 948, so height 948-324 = 624.
        Assert.Equal(LayoutLength.Px(324f), credits.Layout.Window.Top);
        Assert.Equal(LayoutLength.Px(624f), credits.Layout.Window.Height);

        // Row: left VGA x=42 -> 210; name right edge VGA x=277 -> 1385, so right inset
        // 1600-1385 = 215. Height VGA 11 -> 66. Top-aligned (Start), matching the original.
        Assert.Equal(LayoutLength.Px(210f), credits.Layout.Row.Left);
        Assert.Equal(LayoutLength.Px(215f), credits.Layout.Row.Right);
        Assert.Equal(LayoutLength.Px(66f), credits.Layout.Row.Height);
        Assert.NotNull(credits.Layout.Row.Flow);
        Assert.Equal(LayoutFlowDirection.Row, credits.Layout.Row.Flow.Direction);
        Assert.Equal(LayoutFlowJustify.SpaceBetween, credits.Layout.Row.Flow.Justify);
        Assert.Equal(LayoutFlowAlign.Start, credits.Layout.Row.Flow.Align);
        Assert.False(credits.Layout.Row.Flow.Wrap);

        // Paint parameters, unchanged from the coordinate model.
        Assert.Equal(LayoutLength.Px(96f), credits.Layout.FadeTopBand);
        Assert.Equal(LayoutLength.Px(102f), credits.Layout.FadeBottomBand);
        Assert.Equal(LayoutLength.Px(48f), credits.Layout.FontSize);
        Assert.Equal(LayoutLength.Px(20f), credits.Layout.LeaderDotPitch);
        Assert.Equal(LayoutLength.Px(2.5f), credits.Layout.LeaderDotRadius);
        Assert.Equal(LayoutLength.Px(10f), credits.Layout.LeaderGap);

        Assert.NotNull(credits.Frame);
        Assert.Equal(AspectCorrection.CanonicalWidth, credits.Frame.Width);
        Assert.Equal(AspectCorrection.CanonicalHeight, credits.Frame.Height);
        Assert.Equal(LayoutFit.Contain, credits.Frame.Fit);
    }
}
