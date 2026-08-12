namespace ResourceExtraction.Extractors;

using GameData.Resources.Combat;
using System;
using System.IO;

/// <summary>
/// Parses TRAPS.DAT — per-combat-encounter grid layouts. The file is a flat array of fixed
/// 62-byte blocks (u16 count + 15 × {i16 type, u8 gridX, u8 gridY}). The block count is derived
/// from the stream length. The header count is read signed, matching the engine's
/// <c>while (i &lt; recordCount)</c>, so a negative count yields no elements.
///
/// <para><b>An over-long encounter deliberately reads past its own block.</b> Four encounters
/// declare more records than the 15 slots hold — 217 and 349 declare 16, 379 declares 18, 463
/// declares 19 — and <c>combatgrid_load_traps_dat</c> caps nothing: it seeks once and reads
/// <c>count</c> records straight on into the following block. We do the same, because the surplus
/// is not always junk: encounter 463's 16th record is the <c>-18</c> clear-combat-flag marker that
/// makes it a pure puzzle rather than a fight, and clamping to 15 silently dropped it.</para>
///
/// <para>That overrun is also why encounter 464 has a nonsensical count of <c>-18</c>: its block is
/// 463's tail, so what looks like a count is really 463's record. The engine reads it as a count,
/// loops zero times and gets an empty encounter — which is what we emit too.</para>
///
/// <para>See <see cref="TrapData"/> for the semantics and the IDA references
/// (Load_traps.dat @ seg033:0x2e2ce).</para>
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

            // No slot cap: the engine honours the count and runs on into the next block. The only
            // bound is the end of the file, which the engine would have had too.
            int active = Math.Max((int)rawCount, 0);
            for (int e = 0; e < active; e++) {
                if (reader.BaseStream.Position + 4 > resourceStream.Length) {
                    break;
                }
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
