namespace ResourceExtraction.Extractors;

using System.IO;
using GameData.Resources.Data;

/// <summary>
/// Writes one <see cref="SaveGameCombatData"/> record — the inverse of
/// <see cref="CombatRecordReader"/>.
/// </summary>
/// <remarks>
/// <b>The field ORDER is the format.</b> These 22 bytes are the original's <c>CombatantState</c>,
/// written with a plain <c>fwrite</c>, so there is no tag or length to catch a transposition: two
/// adjacent bytes swapped still reads back as a valid record with the wrong meaning. The order here
/// must match <see cref="CombatRecordReader.Read"/> exactly, which is what
/// <c>CombatRecordRoundTripTests</c> pins against the shipped save rather than against this file.
///
/// <para>The signed fields are signed on purpose: <c>PreferredArrowType</c> and
/// <c>FloatingDamageTimer</c> are <c>sbyte</c>, and writing them unsigned would turn -1 (the
/// no-value marker both use) into 255.</para>
/// </remarks>
public static class CombatRecordWriter {
    /// <summary>Bytes per record — the same <c>sizeof(CombatantState)</c> the reader uses.</summary>
    public const int RecordSize = CombatRecordReader.RecordSize;

    /// <summary>Write one record at the writer's current position.</summary>
    public static void Write(BinaryWriter writer, SaveGameCombatData record) {
        writer.Write(record.TargetActorPointer);
        writer.Write(record.CreatureType);
        writer.Write(record.XOnGrid);
        writer.Write(record.YOnGrid);
        writer.Write(record.TargetXOnGrid);
        writer.Write(record.TargetYOnGrid);
        writer.Write(record.CombatStatus);
        writer.Write(record.AnimEffectType);
        writer.Write(record.ActiveSpellEffectSlot);
        writer.Write(record.UnusedPadding);
        writer.Write(record.AnimDurationTimer);
        writer.Write(record.MonsterSpellAbility);
        writer.Write(record.MeleeAttackType);
        writer.Write(record.RangedAttackType);
        writer.Write(record.MovementAiType);
        writer.Write(record.PreferredArrowType);
        writer.Write(record.LastSpellSymbolFile);
        writer.Write(record.FloatingDamageValue);
        writer.Write(record.FloatingDamageTimer);
    }

    /// <summary>The record's bytes, for patching one slot into a save body.</summary>
    public static byte[] ToBytes(SaveGameCombatData record) {
        using var stream = new MemoryStream(RecordSize);
        using (var writer = new BinaryWriter(stream)) {
            Write(writer, record);
        }
        return stream.ToArray();
    }
}
