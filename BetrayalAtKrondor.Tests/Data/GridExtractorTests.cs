namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Combat;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies GRID.DAT parsing: N u16 per-zone combat-grid border pen indices (N derived
/// from file length). See <see cref="GridData"/> / docs/FileFormats/GRID.DAT.md.
/// </summary>
public class GridExtractorTests {

    private static byte[] Build(params ushort[] pens) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        foreach (ushort p in pens) {
            w.Write(p);
        }
        return ms.ToArray();
    }

    private static GridData Extract(params ushort[] pens) =>
        new GridExtractor().Extract("GRID.DAT", new MemoryStream(Build(pens)));

    [Fact]
    public void Extract_ReadsOnePenPerZoneInOrder() {
        GridData result = Extract(224, 224, 234, 225, 187, 152, 173);

        Assert.Equal(7, result.ZoneBorderPens.Count);
        Assert.Equal(224, result.ZoneBorderPens[0]);
        Assert.Equal(234, result.ZoneBorderPens[2]);
        Assert.Equal(152, result.ZoneBorderPens[5]);
    }

    [Fact]
    public void Extract_ReadsTwelveZonesFromFullFile() {
        var pens = new ushort[GridData.ZoneCount];
        for (int i = 0; i < pens.Length; i++) {
            pens[i] = (ushort)(200 + i);
        }
        GridData result = Extract(pens);

        Assert.Equal(GridData.ZoneCount, result.ZoneBorderPens.Count);
        Assert.Equal(211, result.ZoneBorderPens[11]);
    }
}
