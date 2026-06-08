namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Credits;

using ResourceExtraction.Extractors;

using System.IO;
using System.Linq;
using System.Text;

using Xunit;

/// <summary>
/// Verifies CRED.DAT parsing against the format reverse-engineered from
/// LoadCRED.DAT (KRONDOR.EXE 0x40520) and the layout/easter-egg predicates in
/// scrollCredits (0x405f1):
///   uint16 count; uint16 offsets[count]; uint16 blobSize; byte text[blobSize]
/// Entry [0] is the title; the body is consumed as (role, name) pairs. Three
/// content-derived flags are folded in at extract time so the Unity renderer
/// stays free of byte-pattern logic:
///   RareReveal — role begins "LO"  (the hidden 'LOVELY LADY:' credit)
///   Sparkle    — name[0]=='N' &amp;&amp; name[5]=='B'  (the 'Nels Bruckner' sparkle)
///   Centered   — the final block (left index > count-5), centered not columnar
/// </summary>
public class CredExtractorTests {

    /// <summary>Builds a CRED.DAT blob from a string table, matching LoadCRED.DAT.</summary>
    private static byte[] BuildCredDat(string[] strings) {
        var blob = new MemoryStream();
        var offsets = new ushort[strings.Length];
        for (var i = 0; i < strings.Length; i++) {
            offsets[i] = (ushort)blob.Position;
            byte[] bytes = Encoding.ASCII.GetBytes(strings[i]);
            blob.Write(bytes, 0, bytes.Length);
            blob.WriteByte(0);
        }
        byte[] blobBytes = blob.ToArray();

        var output = new MemoryStream();
        var writer = new BinaryWriter(output);
        writer.Write((ushort)strings.Length);
        foreach (ushort offset in offsets) {
            writer.Write(offset);
        }
        writer.Write((ushort)blobBytes.Length);
        writer.Write(blobBytes);
        return output.ToArray();
    }

    // A synthetic table exercising every code path. count = 17, so count-5 = 12:
    // the last two pairs (left index 13 and 15) are the centered closing block.
    private static readonly string[] Sample = {
        /* 0  */ "CREDITS",
        /* 1  */ "PROGRAMMING:",      /* 2  */ "Steve Cordon",
        /* 3  */ "",                  /* 4  */ "Timothy Strelchun",   // continuation
        /* 5  */ "",                  /* 6  */ "",                    // spacer
        /* 7  */ "LEAD PROGRAMMER:",  /* 8  */ "Nels Bruckner",      // sparkle name
        /* 9  */ "LOVELY LADY:",      /* 10 */ "Tawny Reynolds",     // rare-reveal role
        /* 11 */ "",                  /* 12 */ "",                    // spacer
        /* 13 */ "Based on the Midkemian Universe created", /* 14 */ "", // centered
        /* 15 */ "by Midkemia Press and Raymond E. Feist",  /* 16 */ "", // centered
    };

    private static CreditsData Extract(string[] strings) =>
        new CredExtractor().Extract("CRED.DAT", new MemoryStream(BuildCredDat(strings)));

    [Fact]
    public void Extract_TitleIsFirstEntry() {
        CreditsData credits = Extract(Sample);
        Assert.Equal("CREDITS", credits.Title);
    }

    [Fact]
    public void Extract_PairsRoleAndName() {
        CreditsData credits = Extract(Sample);
        CreditLine line = credits.Lines[0];
        Assert.Equal("PROGRAMMING:", line.Role);
        Assert.Equal("Steve Cordon", line.Name);
    }

    [Fact]
    public void Extract_ContinuationLineHasBlankRole() {
        CreditsData credits = Extract(Sample);
        CreditLine line = credits.Lines[1];
        Assert.Equal("", line.Role);
        Assert.Equal("Timothy Strelchun", line.Name);
    }

    [Fact]
    public void Extract_SpacerLineIsFullyBlank() {
        CreditsData credits = Extract(Sample);
        CreditLine line = credits.Lines[2];
        Assert.Equal("", line.Role);
        Assert.Equal("", line.Name);
    }

    [Fact]
    public void Extract_BodyPairCountIsHalfOfBodyEntries() {
        CreditsData credits = Extract(Sample);
        // 16 body entries (indices 1..16) → 8 pairs.
        Assert.Equal(8, credits.Lines.Count);
    }

    [Fact]
    public void Extract_SparkleFlagOnlyOnNxxxBName() {
        CreditsData credits = Extract(Sample);
        Assert.True(credits.Lines.Single(l => l.Name == "Nels Bruckner").Sparkle);
        Assert.All(credits.Lines.Where(l => l.Name != "Nels Bruckner"),
            l => Assert.False(l.Sparkle));
    }

    [Fact]
    public void Extract_RareRevealFlagOnlyOnLoRole() {
        CreditsData credits = Extract(Sample);
        Assert.True(credits.Lines.Single(l => l.Role == "LOVELY LADY:").RareReveal);
        Assert.All(credits.Lines.Where(l => l.Role != "LOVELY LADY:"),
            l => Assert.False(l.RareReveal));
    }

    [Fact]
    public void Extract_CenteredFlagOnlyOnClosingBlock() {
        CreditsData credits = Extract(Sample);
        Assert.True(credits.Lines.Single(l => l.Role.StartsWith("Based on")).Centered);
        Assert.True(credits.Lines.Single(l => l.Role.StartsWith("by Midkemia")).Centered);
        Assert.All(credits.Lines.Where(l => !l.Role.StartsWith("Based on") && !l.Role.StartsWith("by Midkemia")),
            l => Assert.False(l.Centered));
    }

    // ───────────────── Real shipped data (skips when absent) ─────────────────

    private static string? FindRealCredDat() {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", "CRED.DAT");
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [Fact]
    public void Extract_RealCredDat_HasExpectedContent() {
        string? path = FindRealCredDat();
        if (path == null) {
            return; // shipped art not present (e.g. CI) — synthetic tests cover the logic
        }

        using FileStream stream = File.OpenRead(path);
        CreditsData credits = new CredExtractor().Extract("CRED.DAT", stream);

        Assert.Equal("CREDITS", credits.Title);
        // 427 strings: 1 title + 426 body entries → 213 pairs.
        Assert.Equal(213, credits.Lines.Count);
        Assert.Contains(credits.Lines, l => l.Role == "PROGRAMMING:" && l.Name == "Steve Cordon");
        Assert.Single(credits.Lines, l => l.Role == "LOVELY LADY:" && l.RareReveal);
        // 'Nels Bruckner' is credited twice and sparkles both times; no other name does.
        Assert.Equal(2, credits.Lines.Count(l => l.Sparkle));
        Assert.All(credits.Lines.Where(l => l.Sparkle), l => Assert.Equal("Nels Bruckner", l.Name));
    }
}
