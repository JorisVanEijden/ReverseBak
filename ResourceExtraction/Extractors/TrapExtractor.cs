namespace ResourceExtraction.Extractors;

using GameData.Resources.Combat;
using System;
using System.IO;

/// <summary>
/// Parses TRAPS.DAT — per-combat-encounter grid layouts. The file is a flat array of fixed
/// 62-byte records (u16 count + 15 × {i16 type, u8 gridX, u8 gridY}). The record count is
/// derived from the stream length. The header count is read signed (matching the engine,
/// which loops <c>while (short)i &lt; (short)count</c>) and clamped to the 15 physical slots;
/// only the active elements are emitted. See <see cref="TrapData"/> for the semantics and
/// the IDA references (Load_traps.dat @ seg033:0x2e2ce).
/// </summary>
public class TrapExtractor : ExtractorBase<TrapData> {
    private const int RecordBytes = 2 + TrapData.SlotsPerEncounter * 4; // 62

    public override TrapData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new TrapData(id);

        int recordCount = (int)(resourceStream.Length / RecordBytes);
        for (int r = 0; r < recordCount; r++) {
            long recordStart = (long)r * RecordBytes;
            reader.BaseStream.Seek(recordStart, SeekOrigin.Begin);

            short rawCount = reader.ReadInt16();
            var encounter = new TrapEncounter {
                Index = r,
                Key = GameData.Resources.Content.ContentKey.ForBase("traps", r),
                RawCount = rawCount,
            };

            int active = Math.Clamp((int)rawCount, 0, TrapData.SlotsPerEncounter);
            for (int e = 0; e < active; e++) {
                encounter.Elements.Add(new TrapElement {
                    Type = reader.ReadInt16(),
                    GridX = reader.ReadByte(),
                    GridY = reader.ReadByte(),
                });
            }

            data.Encounters.Add(encounter);
        }

        return data;
    }
}
