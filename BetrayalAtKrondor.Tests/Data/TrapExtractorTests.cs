namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Combat;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies TRAPS.DAT parsing: fixed 62-byte encounter records (u16 count + 15 ×
/// {i16 type, u8 gridX, u8 gridY}); count read signed and clamped to 15; only active
/// elements emitted. See <see cref="TrapData"/> / docs/FileFormats/TRAPS.DAT.md.
/// </summary>
public class TrapExtractorTests {

    private const int RecordBytes = 62;

    // Builds one 62-byte record: count header + up to 15 element slots (zero-padded).
    private static byte[] Record(short count, params (short type, byte x, byte y)[] slots) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(count);
        foreach ((short type, byte x, byte y) in slots) {
            w.Write(type);
            w.Write(x);
            w.Write(y);
        }
        // pad to the fixed 62-byte record size
        while (ms.Length < RecordBytes) {
            ms.WriteByte(0);
        }
        return ms.ToArray();
    }

    private static TrapData Extract(params byte[][] records) {
        var ms = new MemoryStream();
        foreach (byte[] rec in records) {
            ms.Write(rec, 0, rec.Length);
        }
        ms.Position = 0;
        return new TrapExtractor().Extract("TRAPS.DAT", ms);
    }

    [Fact]
    public void Extract_ReadsActiveElementsPerEncounter() {
        TrapData result = Extract(
            Record(2, (195, 1, 6), (-15, 2, 0)),
            Record(0));

        Assert.Equal(2, result.Encounters.Count);

        TrapEncounter e0 = result.Encounters[0];
        Assert.Equal(0, e0.Index);
        Assert.Equal(2, e0.RawCount);
        Assert.Equal(2, e0.Elements.Count);
        Assert.Equal(195, e0.Elements[0].Type);
        Assert.Equal(1, e0.Elements[0].GridX);
        Assert.Equal(6, e0.Elements[0].GridY);
        Assert.Equal((int)TrapElementType.ActorSlot0, e0.Elements[1].Type);

        Assert.Empty(result.Encounters[1].Elements);
    }

    [Fact]
    public void Extract_IgnoresStaleSlotsBeyondCount() {
        // count=1 but two slots populated — only the first is active.
        TrapData result = Extract(Record(1, (7, 0, 2), (8, 4, 2)));

        Assert.Single(result.Encounters[0].Elements);
        Assert.Equal((int)TrapElementType.RedCrystal, result.Encounters[0].Elements[0].Type);
    }

    [Fact]
    public void Extract_TreatsNegativeCountAsEmpty() {
        // 0xFFEE reads as a negative short -> zero active elements (engine compares signed).
        TrapData result = Extract(Record(unchecked((short)0xFFEE), (7, 0, 0)));

        Assert.Equal(-18, result.Encounters[0].RawCount);
        Assert.Empty(result.Encounters[0].Elements);
    }

    [Fact]
    public void Extract_ClampsOversizedCountToFifteenSlots() {
        var slots = new (short, byte, byte)[15];
        for (int i = 0; i < 15; i++) {
            slots[i] = ((short)7, (byte)i, (byte)0);
        }
        TrapData result = Extract(Record(19, slots)); // claims 19, only 15 fit

        Assert.Equal(19, result.Encounters[0].RawCount);
        Assert.Equal(15, result.Encounters[0].Elements.Count);
    }
}
