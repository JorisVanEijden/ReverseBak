namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Base for DEF_*.DAT extractors. Mirrors LoadEntryFromDefFile @ ovr187:0x750d9.
//
// On-disk format:
//   u32 count
//   for each record:
//     u8 status    (1 = present; 0 = vacant — original loader returns false)
//     payload      (PayloadSize bytes, per-format layout)
//
// Status=0 records appear in shipping data for several formats (notably
// DEF_COMB, DEF_BLOC, DEF_ZONE, DEF_TOWN, DEF_BKGR). Their payload bytes
// on disk may be stale and should not be interpreted unless Status=1.
// We read both status and payload for every record; consumers gate on
// DefRecord.Status before treating Payload fields as meaningful.
//
// See docs/FileFormats/DEF_DAT family.md for the full format documentation.
public abstract class DefFamilyExtractorBase<TEntry> : ExtractorBase<DefFamilyFile<TEntry>> {
    protected abstract int PayloadSize { get; }
    protected abstract TEntry ReadPayload(BinaryReader reader);

    public override DefFamilyFile<TEntry> Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        uint count = reader.ReadUInt32();
        long expectedBodyLength = (long)count * (PayloadSize + 1);
        long actualBodyLength = resourceStream.Length - 4;
        if (expectedBodyLength != actualBodyLength) {
            throw new InvalidDataException(
                $"{id}: header says {count} records of (1 status + {PayloadSize}) bytes ({expectedBodyLength}) " +
                $"but body is {actualBodyLength} bytes");
        }

        var records = new List<DefRecord<TEntry>>((int)count);
        for (uint i = 0; i < count; i++) {
            byte status = reader.ReadByte();
            TEntry payload = ReadPayload(reader);
            records.Add(new DefRecord<TEntry> { Status = status, Payload = payload });
        }
        return new DefFamilyFile<TEntry>(id, records);
    }
}
