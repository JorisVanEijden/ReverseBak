namespace ResourceExtraction.Extractors.Exe;

using GameData.Resources.Combat;
using System;

/// <summary>
/// Reads the three combat affinity tables out of <c>KRONDOR.EXE</c>'s resident data.
///
/// <para>Unlike <see cref="ExeStringReader"/> these tables are numeric, so there is no text to anchor
/// on. Instead the file offset is <b>computed</b> from the MZ header:</para>
/// <code>fileOffset = idaLinearAddress - IdaLoadBias + headerParagraphs * 16</code>
/// <para>which is the same <c>+0x10000</c> load bias <c>docs/2026-07-26-full-function-map.md</c>
/// documents for every other address in the IDA database. Nothing is hardcoded to one build: the
/// header supplies its own size.</para>
///
/// <para>Because a wrong offset would silently yield plausible-looking numbers rather than an error,
/// the class-group modifier's known byte signature is checked first and the read is refused if it
/// does not match. A loud failure beats a table of quiet nonsense feeding combat balance.</para>
/// </summary>
public static class CombatAffinityReader {
    /// <summary>IDA loads this image at a 0x10000 bias over the file's post-header bytes.</summary>
    public const int IdaLoadBias = 0x10000;

    /// <summary><c>racialMods?</c> / <c>g_aClassGroupModifier[3][4]</c>.</summary>
    public const int ClassGroupModifierAddress = 0x3B646;

    /// <summary><c>creatureWeaknessFlags</c> / <c>g_aClassProficiencyMask[64]</c>.</summary>
    public const int WeaknessAddress = 0x3B6D4;

    /// <summary><c>creatureResistanceFlags</c> / <c>g_aClassWeaknessMask[64]</c>.</summary>
    public const int ResistanceAddress = 0x3B754;

    /// <summary><c>g_anStatCheckThreshold[9]</c>, the can-cast health thresholds.</summary>
    public const int StatCheckThresholdAddress = 0x3B246;
    public const int StatCheckThresholdCount = 9;

    /// <summary><c>g_ai_flee_threshold_table[10]</c>, the morale check's flee chances. Sits
    /// immediately after the stat thresholds, which is a useful cross-check on both.</summary>
    public const int AiFleeThresholdAddress = 0x3B258;
    public const int AiFleeThresholdCount = 10;

    // The shipping 1.02 CD values of the class-group modifier, used purely as a placement check.
    private static readonly short[] ExpectedModifiers = {
        0, -1, -1, -2,
        -1, 0, -1, -2,
        -1, -1, 0, -2,
    };

    /// <summary>Converts an IDA linear address to a file offset using the image's own MZ header.</summary>
    public static int FileOffset(byte[] exe, int idaAddress) {
        if (exe == null) {
            throw new ArgumentNullException(nameof(exe));
        }
        if (exe.Length < 0x20 || exe[0] != (byte)'M' || exe[1] != (byte)'Z') {
            throw new InvalidOperationException("Not an MZ executable.");
        }
        int headerParagraphs = exe[8] | (exe[9] << 8);
        return idaAddress - IdaLoadBias + (headerParagraphs * 16);
    }

    /// <summary>Reads all three tables.</summary>
    public static CombatAffinityTables Read(byte[] exe, string id = "KRONDOR.EXE") {
        int modifierOffset = FileOffset(exe, ClassGroupModifierAddress);
        var table = new CombatAffinityTables(id);

        var modifiers = new int[CombatAffinityTables.ClassGroups][];
        for (var group = 0; group < CombatAffinityTables.ClassGroups; group++) {
            modifiers[group] = new int[CombatAffinityTables.ItemGroups];
            for (var item = 0; item < CombatAffinityTables.ItemGroups; item++) {
                int index = (group * CombatAffinityTables.ItemGroups) + item;
                short value = ReadInt16(exe, modifierOffset + (index * 2));
                if (value != ExpectedModifiers[index]) {
                    throw new InvalidOperationException(
                        $"Class-group modifier at IDA 0x{ClassGroupModifierAddress:X} (file 0x{modifierOffset:X}) "
                        + $"does not match the known table at index {index}: got {value}, expected "
                        + $"{ExpectedModifiers[index]}. The address rule or the executable differs from "
                        + "the 1.02 CD build this was reversed against — refusing to emit a guess.");
                }
                modifiers[group][item] = value;
            }
        }
        table.ClassGroupModifier = modifiers;

        table.StatCheckThresholds = ReadInt16Array(exe, FileOffset(exe, StatCheckThresholdAddress),
            StatCheckThresholdCount);
        table.AiFleeThresholds = ReadInt16Array(exe, FileOffset(exe, AiFleeThresholdAddress),
            AiFleeThresholdCount);

        int weaknessOffset = FileOffset(exe, WeaknessAddress);
        int resistanceOffset = FileOffset(exe, ResistanceAddress);
        for (var classId = 0; classId < CombatAffinityTables.CreatureClassCount; classId++) {
            table.Creatures.Add(new CreatureAffinity {
                ClassId = classId,
                WeaknessFlags = ReadUInt16(exe, weaknessOffset + (classId * 2)),
                ResistanceFlags = ReadUInt16(exe, resistanceOffset + (classId * 2)),
            });
        }
        return table;
    }

    private static short ReadInt16(byte[] exe, int offset) => (short)ReadUInt16(exe, offset);

    private static int[] ReadInt16Array(byte[] exe, int offset, int count) {
        var values = new int[count];
        for (var i = 0; i < count; i++) {
            values[i] = ReadInt16(exe, offset + (i * 2));
        }
        return values;
    }

    private static int ReadUInt16(byte[] exe, int offset) {
        if (offset < 0 || offset + 1 >= exe.Length) {
            throw new InvalidOperationException($"Offset 0x{offset:X} is outside the executable.");
        }
        return exe[offset] | (exe[offset + 1] << 8);
    }
}
