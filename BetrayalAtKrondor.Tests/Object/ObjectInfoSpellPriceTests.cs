namespace BetrayalAtKrondor.Tests.Object;

using GameData.Resources.Object;
using ResourceExtraction.Extractors.Object;
using System.IO;
using Xunit;

/// <summary>
/// The scroll price table that follows OBJINFO.DAT's 138 item records.
/// </summary>
/// <remarks>
/// <c>itemtbl_compute_value</c> (ITEMTBL.C:66) prices a Magical Scroll as
/// <c>((int far *)(g_pItemDefTable + 0x2b20))[slot->condition]</c>, and a scroll's condition byte
/// is the spell number. 138 records x 80 bytes = 11040 = 0x2b20, so the table starts immediately
/// after them. See docs/shop-pricing.md line 60.
/// </remarks>
public class ObjectInfoSpellPriceTests {
    private const int RecordCount = 138;
    private const int RecordSize = 80;

    // The extractor reads names in codepage 437, which .NET Core does not carry by default. The
    // app registers the provider at startup; a test that calls Extract cold has to do the same or
    // it throws NotSupportedException before reaching anything worth asserting.
    static ObjectInfoSpellPriceTests() {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    /// <summary>The table is read from 0x2b20 and indexed by spell number, not by position.</summary>
    [Fact]
    public void PricesAreReadFromTheEndOfTheRecordsAndIndexedBySpellNumber() {
        var bytes = new byte[RecordCount * RecordSize + 45 * 2];
        // spell 0 = 1500, spell 3 = 700, spell 44 = 4321 — the ends and one in the middle.
        void Put(int spell, short value) {
            byte[] v = System.BitConverter.GetBytes(value);
            bytes[RecordCount * RecordSize + spell * 2] = v[0];
            bytes[RecordCount * RecordSize + spell * 2 + 1] = v[1];
        }
        Put(0, 1500);
        Put(3, 700);
        Put(44, 4321);

        ObjectInfoSet set = new ObjectInfoSetExtractor().Extract("OBJINFO.DAT", new MemoryStream(bytes));

        Assert.Equal(45, set.SpellPrices.Count);
        Assert.Equal(1500, set.SpellPriceFor(0));
        Assert.Equal(700, set.SpellPriceFor(3));
        Assert.Equal(4321, set.SpellPriceFor(44));
        Assert.Equal(0, set.SpellPriceFor(1));
    }

    /// <summary>Out-of-range asks answer 0 rather than throwing — a scroll can carry any byte.</summary>
    [Fact]
    public void AnOutOfRangeSpellNumberIsZeroNotAnException() {
        var set = new ObjectInfoSet("O", new System.Collections.Generic.List<ObjectInfo>(),
                                    new[] { 10, 20, 30 });
        Assert.Equal(0, set.SpellPriceFor(-1));
        Assert.Equal(0, set.SpellPriceFor(3));
        Assert.Equal(0, set.SpellPriceFor(255));
        Assert.Equal(20, set.SpellPriceFor(1));
    }

    /// <summary>A file that stops after the records yields no prices instead of throwing.</summary>
    [Fact]
    public void AFileWithoutTheTrailingTableYieldsNoPrices() {
        var bytes = new byte[RecordCount * RecordSize];

        ObjectInfoSet set = new ObjectInfoSetExtractor().Extract("OBJINFO.DAT", new MemoryStream(bytes));

        Assert.Empty(set.SpellPrices);
        Assert.Equal(0, set.SpellPriceFor(3));
    }
}
