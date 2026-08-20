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
        TimeSnapshot: BitConverter.ToInt32(body, SaveGameOffsets.TimeSnapshot),
        PaletteEventMask: BitConverter.ToInt16(body, SaveGameOffsets.PaletteEventMask),
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
        // chapter2 + gold4 + time4 + snapshot4 + palEventMask2 + zone1 + worldX1 + worldY1
        // + posX4 + posY4 + posZ4 + rot2 = 33.
        // Scalars only — this call passes no container/actor/timer edits, which are covered
        // separately. Raise this number deliberately as more of the block is modelled; that is
        // what makes coverage growth visible rather than accidental.
        const int ModelledScalarBytes = 33;
        Assert.Equal(ModelledScalarBytes, r.Coverage.AuthoredBytes);
        Assert.Equal(SaveGameOffsets.BodySize, r.Coverage.TotalBodyBytes);
        Assert.Equal(SaveGameOffsets.BodySize - ModelledScalarBytes, r.Coverage.PassthroughBytes);
    }

    private static bool IsAuthored(SaveCoverage cov, int offset) {
        foreach (var (o, len) in cov.AuthoredRanges) {
            if (offset >= o && offset < o + len) { return true; }
        }
        return false;
    }

    [Fact]
    public void TheAutomapBlockIsWrittenIntoTheBodyAndReadsBack() {
        byte[] body = PatternBody();
        var visits = new GameData.Resources.World.EncounterVisitTable();
        visits.MarkSeen(11, 13, 9, 42);
        visits.MarkSeen(12, 2, 3, 299);    // the highest index a 300-record tile can produce

        SaveGameWriteResult r = SaveGameWriter.Write(
            body, FieldsFrom(body), "Slot A", 40, 41, 3, automapVisits: visits);
        byte[] outBody = r.Bytes[SaveGameOffsets.HeaderSize..];

        var reloaded = new GameData.Resources.World.EncounterVisitTable();
        reloaded.Load(outBody);
        Assert.True(reloaded.HasSeen(11, 13, 9, 42));
        Assert.True(reloaded.HasSeen(12, 2, 3, 299));

        // It lands in the body, so in the FILE it appears past the 100-byte header — the confusion
        // that made the first cut of this read the wrong 0x668 bytes.
        Assert.Equal(GameData.Resources.World.EncounterVisitTable.FileOffset,
            SaveGameOffsets.HeaderSize + GameData.Resources.World.EncounterVisitTable.BodyOffset);
    }

    [Fact]
    public void ATableLoadedFromAFullBlockDropsNewMarks() {
        // Not a contrivance — the pattern body has no free slots (a free one is three 0xff bytes),
        // so loading it yields the full table the model documents. The original has no eviction, so
        // the forty-first tile is simply never recorded, and this is what that looks like from the
        // outside. Discovered by writing the test above against a pattern body and watching it drop.
        byte[] body = PatternBody();
        var visits = new GameData.Resources.World.EncounterVisitTable();
        visits.Load(body);

        Assert.Equal(GameData.Resources.World.EncounterVisitTable.Capacity, visits.UsedSlots);
        Assert.False(visits.MarkSeen(11, 13, 9, 42));
        Assert.False(visits.HasSeen(11, 13, 9, 42));
    }

    [Fact]
    public void WithoutAnAutomapTableTheBlockIsPassedThroughUntouched() {
        // Every other save path must stay byte-identical, including the block we now know how to
        // write — passing no table means "leave whatever the backing body had".
        byte[] body = PatternBody();
        SaveGameWriteResult r = SaveGameWriter.Write(body, FieldsFrom(body), "Slot A", 40, 41, 3);
        byte[] outBody = r.Bytes[SaveGameOffsets.HeaderSize..];

        int at = GameData.Resources.World.EncounterVisitTable.BodyOffset;
        int size = GameData.Resources.World.EncounterVisitTable.SaveSize;
        Assert.Equal(body[at..(at + size)], outBody[at..(at + size)]);
    }

    [Fact]
    public void ThePaletteEventMaskRoundTripsAtTheOffsetGstateGivesIt() {
        // wPalEventMask sits immediately after the 160-byte timer pool. Pinned against the body so
        // a shift in the pool's size cannot move it silently.
        Assert.Equal(SaveGameOffsets.TimerPool + (8 * 20), SaveGameOffsets.PaletteEventMask);

        byte[] body = PatternBody();
        SaveGameFields fields = FieldsFrom(body) with { PaletteEventMask = 0x0105 };
        SaveGameWriteResult r = SaveGameWriter.Write(body, fields, "C", 40, 41, 3);
        byte[] outBody = r.Bytes[SaveGameOffsets.HeaderSize..];

        Assert.Equal(0x0105, BitConverter.ToInt16(outBody, SaveGameOffsets.PaletteEventMask));
    }

    [Fact]
    public void ThePaletteMaskIsWhatTheParserCallsActiveSpellTimerFlags() {
        // The parser already read this word — as the LIGHTING block's first field. Naming it that
        // is why the save looked as though it did not carry the palette mask at all.
        byte[] body = PatternBody();
        SaveGameFields fields = FieldsFrom(body) with { PaletteEventMask = 0x00A0 };
        SaveGameWriteResult r = SaveGameWriter.Write(body, fields, "C", 40, 41, 3);

        SaveGame reloaded = new SaveGameExtractor().Extract(
            "SAVE.GAM", new MemoryStream(r.Bytes));

        Assert.Equal((short)0x00A0, reloaded.Data!.StateData.LightingStateData.ActiveSpellTimerFlags);
    }
}
