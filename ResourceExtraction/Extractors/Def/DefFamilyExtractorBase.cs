namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System;
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
// In all surveyed shipping data, every record has status=1; the per-record
// gate is dormant. We read the status byte, log if it's anything other than
// 1, and always read the payload (the bytes are on disk regardless).
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

        var entries = new List<TEntry>((int)count);
        for (uint i = 0; i < count; i++) {
            byte status = reader.ReadByte();
            TEntry payload = ReadPayload(reader);
            if (status != 1) {
                Console.Error.WriteLine($"[DEF] {id} record {i}: status = {status} (expected 1)");
            }
            entries.Add(payload);
        }
        return new DefFamilyFile<TEntry>(id, entries);
    }
}
