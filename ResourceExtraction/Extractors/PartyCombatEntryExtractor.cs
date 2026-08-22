namespace ResourceExtraction.Extractors;

using System.Collections.Generic;
using System.IO;
using System.Text;
using GameData.Resources.Combat;
using GameData.Resources.Data;

/// <summary>
/// Parses <c>P1.DAT</c> — the party's combat entry states, one 22-byte
/// <c>CombatantState</c> per character slot.
/// </summary>
/// <remarks>
/// Reuses <see cref="CombatRecordReader"/> rather than re-describing the layout: this is the same
/// record the save's combat section holds, and two copies of a 19-field reader would drift.
/// </remarks>
public class PartyCombatEntryExtractor : ExtractorBase<PartyCombatEntries> {
    public override PartyCombatEntries Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.UTF8, leaveOpen: true);
        var slots = new List<SaveGameCombatData>(PartyCombatEntries.SlotCount);

        // Read what is actually there rather than assuming six: a short file yields fewer slots
        // instead of throwing partway through a record.
        while (resourceStream.Length - resourceStream.Position >= CombatRecordReader.RecordSize) {
            slots.Add(CombatRecordReader.Read(reader));
        }

        return new PartyCombatEntries(id, slots);
    }
}
