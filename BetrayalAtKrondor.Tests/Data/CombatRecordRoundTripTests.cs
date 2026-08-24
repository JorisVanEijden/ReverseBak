namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;

using ResourceExtraction;
using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// <c>CombatRecordWriter</c> against <c>CombatRecordReader</c>, over the SHIPPED save.
/// </summary>
/// <remarks>
/// <b>Pinned against real bytes rather than against a record this test built.</b> A round trip of my
/// own object proves the two halves agree with each other, which they would even if both had the
/// same field transposed. The engine's own bytes are the only thing that can catch that — and the
/// combat block has no tag or length, so a transposition reads back as a perfectly valid record with
/// the wrong meaning.
/// </remarks>
public class CombatRecordRoundTripTests {
    static CombatRecordRoundTripTests() =>
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    /// <summary>Where the combat block starts in a save BODY.</summary>
    private static int BlockOffset =>
        SaveGameOffsets.StateDataSize + SaveGameOffsets.WorldDataSize + SaveGameOffsets.ActorDataSize;

    [Fact]
    public void EveryShippedCombatRecordSurvivesAWriteUnchanged() {
        byte[]? save = ReadGameFile(Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        if (save == null) {
            return;   // skip-if-absent, like the other game-data tests
        }

        // The body follows the 100-byte header.
        int bodyStart = save.Length - SaveGameOffsets.BodySize;
        int start = bodyStart + BlockOffset;
        int records = SaveGameOffsets.CombatDataSize / CombatRecordWriter.RecordSize;
        Assert.Equal(1730, records);   // the actor table's count — one combat slot per actor

        var mismatches = 0;
        for (var i = 0; i < records; i++) {
            int at = start + (i * CombatRecordWriter.RecordSize);
            using var stream = new MemoryStream(save, at, CombatRecordWriter.RecordSize);
            using var reader = new BinaryReader(stream);
            SaveGameCombatData record = CombatRecordReader.Read(reader);

            byte[] written = CombatRecordWriter.ToBytes(record);
            for (var b = 0; b < CombatRecordWriter.RecordSize; b++) {
                if (written[b] != save[at + b]) {
                    mismatches++;
                    break;
                }
            }
        }

        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void TheSignedFieldsKeepTheirNoValueMarker() {
        // -1 is the marker on both sbyte fields; writing them unsigned turns it into 255, which the
        // round trip above would NOT catch if the shipped save happens to carry none.
        var record = new SaveGameCombatData(
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            preferredArrowType: -1, lastSpellSymbolFile: 0, floatingDamageValue: 0,
            floatingDamageTimer: -1);

        byte[] bytes = CombatRecordWriter.ToBytes(record);
        using var reader = new BinaryReader(new MemoryStream(bytes));
        SaveGameCombatData back = CombatRecordReader.Read(reader);

        Assert.Equal(-1, back.PreferredArrowType);
        Assert.Equal(-1, back.FloatingDamageTimer);
    }

    [Fact]
    public void PatchingOneSlotLeavesEveryOtherByteAlone() {
        // The writer's whole contract is preserve-and-patch: an unmodelled or untouched region must
        // stay byte-identical to what the engine wrote. A slot write that clobbered its neighbours
        // would still round-trip THAT slot correctly, so this asserts the bytes AROUND it.
        byte[]? save = ReadGameFile(Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        if (save == null) {
            return;
        }

        int bodyStart = save.Length - SaveGameOffsets.BodySize;
        var body = new byte[SaveGameOffsets.BodySize];
        System.Buffer.BlockCopy(save, bodyStart, body, 0, SaveGameOffsets.BodySize);

        const int slot = 400;
        var record = new SaveGameCombatData(
            0, creatureType: 7, xOnGrid: 3, yOnGrid: 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            preferredArrowType: -1, lastSpellSymbolFile: 0, floatingDamageValue: 0,
            floatingDamageTimer: -1);

        SaveGameWriteResult result = SaveGameWriter.Write(
            body, default, "test", 0, 0, 0,
            combatantEdits: new[] { new DirtyCombatantEdit(slot, record) });

        int at = SaveGameOffsets.CombatDataOffset + (slot * CombatRecordWriter.RecordSize);
        byte[] written = result.Bytes;
        int writtenBodyStart = written.Length - SaveGameOffsets.BodySize;

        // The slot itself changed to what we asked for...
        byte[] expected = CombatRecordWriter.ToBytes(record);
        for (var b = 0; b < CombatRecordWriter.RecordSize; b++) {
            Assert.Equal(expected[b], written[writtenBodyStart + at + b]);
        }

        // ...and the records on either side did not.
        for (var b = -CombatRecordWriter.RecordSize; b < 0; b++) {
            Assert.Equal(body[at + b], written[writtenBodyStart + at + b]);
        }
        for (var b = CombatRecordWriter.RecordSize; b < CombatRecordWriter.RecordSize * 2; b++) {
            Assert.Equal(body[at + b], written[writtenBodyStart + at + b]);
        }
    }

    [Fact]
    public void ASlotOutsideTheActorTableIsRefusedRatherThanWrittenSomewhereElse() {
        // Unchecked, slot 1730 writes 22 bytes into whatever follows the combat block.
        byte[] body = new byte[SaveGameOffsets.BodySize];
        var record = new SaveGameCombatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => SaveGameWriter.Write(
            body, default, "test", 0, 0, 0,
            combatantEdits: new[] { new DirtyCombatantEdit(SaveGameOffsets.CombatSlotCount, record) }));
    }

    [Fact]
    public void TheActivePartyIsWrittenAsSizeAndMembersTogether() {
        byte[] body = new byte[SaveGameOffsets.BodySize];

        SaveGameWriteResult r = SaveGameWriter.Write(
            body, new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                ActiveParty: new byte[] { 4, 2 }),
            "test", 0, 0, 0);

        int at = r.Bytes.Length - SaveGameOffsets.BodySize;
        Assert.Equal(2, r.Bytes[at + SaveGameOffsets.ActivePartySize]);
        Assert.Equal(4, r.Bytes[at + SaveGameOffsets.ActivePartyMembers]);
        Assert.Equal(2, r.Bytes[at + SaveGameOffsets.ActivePartyMembers + 1]);
    }

    [Fact]
    public void SpareSlotsAreLEFTALONERatherThanZeroed() {
        // The engine reads only the first `size` slots. Zeroing the rest would claim character 0
        // sits in them — and 0 is a real character.
        var body = new byte[SaveGameOffsets.BodySize];
        body[SaveGameOffsets.ActivePartyMembers + 2] = 0x5a;

        SaveGameWriteResult r = SaveGameWriter.Write(
            body, new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                ActiveParty: new byte[] { 4, 2 }),
            "test", 0, 0, 0);

        int at = r.Bytes.Length - SaveGameOffsets.BodySize;
        Assert.Equal(0x5a, r.Bytes[at + SaveGameOffsets.ActivePartyMembers + 2]);
    }

    [Fact]
    public void NullLeavesTheSavesOwnPartyUntouched() {
        var body = new byte[SaveGameOffsets.BodySize];
        body[SaveGameOffsets.ActivePartySize] = 3;
        body[SaveGameOffsets.ActivePartyMembers] = 9;

        SaveGameWriteResult r = SaveGameWriter.Write(
            body, new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            "test", 0, 0, 0);

        int at = r.Bytes.Length - SaveGameOffsets.BodySize;
        Assert.Equal(3, r.Bytes[at + SaveGameOffsets.ActivePartySize]);
        Assert.Equal(9, r.Bytes[at + SaveGameOffsets.ActivePartyMembers]);
    }

    [Fact]
    public void APartyLargerThanTheArrayIsRefused() {
        Assert.Throws<System.ArgumentException>(() => SaveGameWriter.Write(
            new byte[SaveGameOffsets.BodySize],
            new SaveGameFields(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                ActiveParty: new byte[] { 1, 2, 3, 4 }),
            "test", 0, 0, 0));
    }

    private static byte[]? ReadGameFile(string name) {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }
        return null;
    }
}
