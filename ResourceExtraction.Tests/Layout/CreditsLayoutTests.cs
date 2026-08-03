namespace ResourceExtraction.Tests.Layout;

using System.IO;
using System.Text;

using GameData.Resources.Credits;
using GameData.Resources.Layout;

using ResourceExtraction.Extractors;

using Xunit;

/// <summary>Faithfulness gate for the credits layout: these are exactly the constants
/// CreditsView.cs carried before the conversion (see the plan's table). If one of these
/// changes, the credits screen has moved relative to the original.</summary>
public class CreditsLayoutTests {
    [Fact]
    public void Defaults_MatchTheOriginalCanonicalGeometry() {
        var layout = new CreditsLayout();
        Assert.Equal(LayoutLength.Px(246f), layout.TitleY);
        Assert.Equal(LayoutLength.Px(324f), layout.WindowTop);
        Assert.Equal(LayoutLength.Px(948f), layout.WindowBottom);
        Assert.Equal(LayoutLength.Px(66f), layout.LineHeight);
        Assert.Equal(LayoutLength.Px(210f), layout.RoleLeftX);
        Assert.Equal(LayoutLength.Px(1385f), layout.NameRightX);
        Assert.Equal(LayoutLength.Px(800f), layout.CenterX);
        Assert.Equal(LayoutLength.Px(96f), layout.FadeTopBand);
        Assert.Equal(LayoutLength.Px(102f), layout.FadeBottomBand);
        Assert.Equal(LayoutLength.Px(48f), layout.FontSize);
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
        Assert.Equal(LayoutLength.Px(246f), credits.Layout.TitleY);
        Assert.Equal(LayoutLength.Px(324f), credits.Layout.WindowTop);
        Assert.Equal(LayoutLength.Px(948f), credits.Layout.WindowBottom);
        Assert.Equal(LayoutLength.Px(66f), credits.Layout.LineHeight);
        Assert.Equal(LayoutLength.Px(210f), credits.Layout.RoleLeftX);
        Assert.Equal(LayoutLength.Px(1385f), credits.Layout.NameRightX);
        Assert.Equal(LayoutLength.Px(800f), credits.Layout.CenterX);
        Assert.Equal(LayoutLength.Px(96f), credits.Layout.FadeTopBand);
        Assert.Equal(LayoutLength.Px(102f), credits.Layout.FadeBottomBand);
        Assert.Equal(LayoutLength.Px(48f), credits.Layout.FontSize);
        Assert.Equal(LayoutLength.Px(20f), credits.Layout.LeaderDotPitch);
        Assert.Equal(LayoutLength.Px(2.5f), credits.Layout.LeaderDotRadius);
        Assert.Equal(LayoutLength.Px(10f), credits.Layout.LeaderGap);
    }
}
