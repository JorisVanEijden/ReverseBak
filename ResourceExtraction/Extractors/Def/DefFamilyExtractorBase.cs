namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Content;
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

    // 10-byte directional landing position, shared by DEF_COMB and DEF_TRAP.
    protected static LandingPosition ReadLanding(BinaryReader reader) {
        return new LandingPosition {
            FineX     = reader.ReadInt32(),
            FineY     = reader.ReadInt32(),
            RotationZ = reader.ReadUInt16(),
        };
    }

    // 339-byte encounter actor-placement block, shared by DEF_COMB (offset
    // 0x3A) and DEF_TRAP (offset 0x44): slotCount + 7×48-byte EnemySlot +
    // 2-byte trailer. See EncounterActorSetup / docs/FileFormats/DEF_DAT family.md.
    protected static EncounterActorSetup ReadEnemySetup(BinaryReader reader) {
        var setup = new EncounterActorSetup { SlotCount = reader.ReadByte() };
        for (int s = 0; s < 7; s++) {
            setup.Slots[s] = new EnemySlot {
                CreatureNumber   = reader.ReadUInt16(),
                MovementPattern  = reader.ReadUInt16(),
                PrimarySpawnX    = reader.ReadInt32(),
                PrimarySpawnY    = reader.ReadInt32(),
                PrimaryRotationZ = reader.ReadInt16(),
                AltSpawnX        = new[] { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() },
                AltSpawnY        = new[] { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() },
                AuthoringWord    = reader.ReadUInt16(),  // 0x2E — runtime-dead structured authoring word (see EnemySlot)
            };
        }
        setup.Trailer = reader.ReadUInt16();
        return setup;
    }

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

        string family = DefFamilySegment(id);
        var records = new List<DefRecord<TEntry>>((int)count);
        for (uint i = 0; i < count; i++) {
            byte status = reader.ReadByte();
            TEntry payload = ReadPayload(reader);
            records.Add(new DefRecord<TEntry> {
                Key = ContentKey.ForBase($"def_{family}", (int)i),
                Status = status,
                Payload = payload,
            });
        }
        return new DefFamilyFile<TEntry>(id, records);
    }

    // "DEF_DIAL.DAT" → "dial"; the de-indexed family segment of a DEF record's content key.
    // Falls back to the whole (lowercased) stem if the id lacks the DEF_ prefix.
    private static string DefFamilySegment(string id) {
        string stem = Path.GetFileNameWithoutExtension(id);
        if (stem.StartsWith("DEF_", System.StringComparison.OrdinalIgnoreCase)) {
            stem = stem.Substring(4);
        }
        return stem.ToLowerInvariant();
    }
}
