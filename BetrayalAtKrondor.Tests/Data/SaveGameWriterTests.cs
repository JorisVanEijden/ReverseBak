namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.IO;
using System.Text;
using GameData.Resources.Data;
using ResourceExtraction;
using ResourceExtraction.Extractors;
using Xunit;

public class SaveGameWriterTests {
    static SaveGameWriterTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // A synthetic backing body with a recognisable pattern so passthrough is easy to verify.
    private static byte[] PatternBody() {
        var body = new byte[SaveGameOffsets.BodySize];
        for (int i = 0; i < body.Length; i++) {
            body[i] = (byte)(i * 31 + 7);
        }
        return body;
    }

    private static SaveGameFields FieldsFrom(byte[] body) => new(
        Chapter: BitConverter.ToInt16(body, SaveGameOffsets.Chapter),
        PartyGold: BitConverter.ToInt32(body, SaveGameOffsets.PartyGold),
        GameTime: BitConverter.ToInt32(body, SaveGameOffsets.GameTime),
        CurrentZone: body[SaveGameOffsets.CurrentZone],
        WorldX: body[SaveGameOffsets.WorldX],
        WorldY: body[SaveGameOffsets.WorldY],
        PositionX: BitConverter.ToInt32(body, SaveGameOffsets.PositionX),
        PositionY: BitConverter.ToInt32(body, SaveGameOffsets.PositionY),
        PositionZ: BitConverter.ToInt32(body, SaveGameOffsets.PositionZ),
        Rotation: BitConverter.ToInt16(body, SaveGameOffsets.Rotation));

    [Fact]
    public void WritingBackUnchangedFields_ProducesAByteIdenticalBody() {
        // The interchangeability contract: read the fields out, write them back -> body unchanged.
        byte[] body = PatternBody();
        SaveGameWriteResult r = SaveGameWriter.Write(body, FieldsFrom(body), "Slot A", 40, 41, 3);

        Assert.Equal(SaveGameOffsets.HeaderSize + SaveGameOffsets.BodySize, r.Bytes.Length);
        byte[] outBody = r.Bytes[SaveGameOffsets.HeaderSize..];
        Assert.Equal(body, outBody);
    }

    [Fact]
    public void Header_IsWrittenExactly() {
        byte[] body = PatternBody();
        var fields = FieldsFrom(body) with { Chapter = 5 };
        SaveGameWriteResult r = SaveGameWriter.Write(body, fields, "Kate", 120, 88, 4);

        using var stream = new MemoryStream(r.Bytes);
        SaveGameHeader h = SaveGameExtractor.ReadHeader(stream);
        Assert.Equal("Kate", h.Name);
        Assert.Equal((short)5, h.ChapterNumber);
        Assert.Equal((short)120, h.WorldX);
        Assert.Equal((short)88, h.WorldY);
        Assert.Equal((short)4, h.MapIcon);
        Assert.Equal(SaveGame.SupportedVersion, h.Version);
    }

    [Fact]
    public void ChangedField_RoundTrips_AndLeavesEverythingElseIdentical() {
        // A zero body keeps ParseData on well-formed (all-default) input; we plant only what we assert.
        byte[] body = new byte[SaveGameOffsets.BodySize];
        var fields = FieldsFrom(body) with { PositionX = 999, Rotation = 256, Chapter = 2 };
        SaveGameWriteResult r = SaveGameWriter.Write(body, fields, "Move", 40, 41, 3);

        using var stream = new MemoryStream(r.Bytes);
        SaveGame reread = new SaveGameExtractor().Extract("x", stream);
        SaveGameStateData s = reread.Data!.StateData;
        Assert.Equal(999, s.PositionX);
        Assert.Equal((short)256, s.CurrentZRotation);
        Assert.Equal((short)2, s.ChapterNumber);

        // Everything outside the authored ranges is byte-identical to the backing body.
        byte[] outBody = r.Bytes[SaveGameOffsets.HeaderSize..];
        for (int i = 0; i < outBody.Length; i++) {
            if (!IsAuthored(r.Coverage, i)) {
                Assert.Equal(body[i], outBody[i]);
            }
        }
    }

    [Fact]
    public void Coverage_CountsExactlyTheModeledScalarBytes() {
        byte[] body = PatternBody();
        SaveGameWriteResult r = SaveGameWriter.Write(body, FieldsFrom(body), "C", 40, 41, 3);
        // chapter2 + gold4 + time4 + zone1 + worldX1 + worldY1 + posX4 + posY4 + posZ4 + rot2 = 27
        Assert.Equal(27, r.Coverage.AuthoredBytes);
        Assert.Equal(SaveGameOffsets.BodySize, r.Coverage.TotalBodyBytes);
        Assert.Equal(SaveGameOffsets.BodySize - 27, r.Coverage.PassthroughBytes);
    }

    private static bool IsAuthored(SaveCoverage cov, int offset) {
        foreach (var (o, len) in cov.AuthoredRanges) {
            if (offset >= o && offset < o + len) { return true; }
        }
        return false;
    }
}
