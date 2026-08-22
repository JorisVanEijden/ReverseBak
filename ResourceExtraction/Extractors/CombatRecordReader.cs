namespace ResourceExtraction.Extractors;

using System.IO;
using GameData.Resources.Data;

/// <summary>
/// Reads one <see cref="SaveGameCombatData"/> record.
///
/// <para><b>The same 22-byte layout appears in two places</b> — the save's combat section (1730
/// records) and <c>P1.DAT</c> (6, one per party slot). It is the original's <c>CombatantState</c>
/// struct in both cases, read with a plain <c>fread</c>, so the two readers must not drift apart.</para>
/// </summary>
public static class CombatRecordReader {
    /// <summary>Bytes per record — <c>sizeof(CombatantState)</c>.</summary>
    public const int RecordSize = 22;

    /// <summary>Read one record at the reader's current position.</summary>
    public static SaveGameCombatData Read(BinaryReader reader) {
        return new SaveGameCombatData(
            reader.ReadUInt16(),
            reader.ReadInt16(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadInt16(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSByte()
        );
    }
}
