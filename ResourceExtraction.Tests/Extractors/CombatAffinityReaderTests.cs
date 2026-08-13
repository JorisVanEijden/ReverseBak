namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Combat;
using ResourceExtraction.Extractors.Exe;
using System;
using Xunit;

/// <summary>
/// The EXE combat affinity tables. The interesting part is the addressing: a wrong offset would
/// produce plausible-looking numbers rather than an error, so the reader checks a known signature
/// and these tests pin both the offset arithmetic and that refusal.
/// </summary>
public class CombatAffinityReaderTests {
    private const int HeaderParagraphs = 576;          // 0x2400 bytes, as in the shipped executable
    private const int HeaderBytes = HeaderParagraphs * 16;

    private static readonly short[] Modifiers = {
        0, -1, -1, -2,
        -1, 0, -1, -2,
        -1, -1, 0, -2,
    };

    /// <summary>Builds a stub image with the three tables at the offsets the address rule predicts.</summary>
    private static byte[] BuildExe(short[] modifiers = null, int[] weakness = null, int[] resistance = null) {
        var exe = new byte[0x40000];
        exe[0] = (byte)'M';
        exe[1] = (byte)'Z';
        exe[8] = HeaderParagraphs & 0xff;
        exe[9] = (HeaderParagraphs >> 8) & 0xff;

        Write(exe, Offset(CombatAffinityReader.ClassGroupModifierAddress), modifiers ?? Modifiers);
        if (weakness != null) {
            WriteU16(exe, Offset(CombatAffinityReader.WeaknessAddress), weakness);
        }
        if (resistance != null) {
            WriteU16(exe, Offset(CombatAffinityReader.ResistanceAddress), resistance);
        }
        return exe;
    }

    private static int Offset(int idaAddress) => idaAddress - CombatAffinityReader.IdaLoadBias + HeaderBytes;

    private static void Write(byte[] exe, int offset, short[] values) {
        for (var i = 0; i < values.Length; i++) {
            exe[offset + (i * 2)] = (byte)(values[i] & 0xff);
            exe[offset + (i * 2) + 1] = (byte)((values[i] >> 8) & 0xff);
        }
    }

    private static void WriteU16(byte[] exe, int offset, int[] values) {
        for (var i = 0; i < values.Length; i++) {
            exe[offset + (i * 2)] = (byte)(values[i] & 0xff);
            exe[offset + (i * 2) + 1] = (byte)((values[i] >> 8) & 0xff);
        }
    }

    [Fact]
    public void TheFileOffsetComesFromTheImagesOwnHeaderRatherThanAConstant() {
        byte[] exe = BuildExe();

        int offset = CombatAffinityReader.FileOffset(exe, CombatAffinityReader.ClassGroupModifierAddress);

        Assert.Equal(0x3B646 - 0x10000 + HeaderBytes, offset);
        Assert.Equal(0x2DA46, offset); // the shipped executable's actual offset
    }

    [Fact]
    public void ADifferentHeaderSizeMovesEveryTableWithIt() {
        var exe = new byte[0x40000];
        exe[0] = (byte)'M';
        exe[1] = (byte)'Z';
        exe[8] = 0x10; // 16 paragraphs = 256 bytes of header

        Assert.Equal(0x3B646 - 0x10000 + 256, CombatAffinityReader.FileOffset(exe, 0x3B646));
    }

    [Fact]
    public void SomethingThatIsNotAnExecutableIsRejected() {
        Assert.Throws<InvalidOperationException>(() => CombatAffinityReader.FileOffset(new byte[64], 0x3B646));
    }

    [Fact]
    public void TheClassGroupModifierIsReadAsThreeRowsOfFour() {
        CombatAffinityTables tables = CombatAffinityReader.Read(BuildExe());

        Assert.Equal(new[] { 0, -1, -1, -2 }, tables.ClassGroupModifier[0]);
        Assert.Equal(new[] { -1, 0, -1, -2 }, tables.ClassGroupModifier[1]);
        Assert.Equal(new[] { -1, -1, 0, -2 }, tables.ClassGroupModifier[2]);
    }

    [Fact]
    public void AMismatchedSignatureIsRefusedRatherThanGuessedAt() {
        // A wrong address would still read *some* numbers; the signature check is what turns that
        // into a failure instead of quietly wrong combat balance.
        var wrong = new short[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

        InvalidOperationException ex =
            Assert.Throws<InvalidOperationException>(() => CombatAffinityReader.Read(BuildExe(wrong)));

        Assert.Contains("refusing to emit a guess", ex.Message);
    }

    [Fact]
    public void EverySixtyFourCreatureClassGetsAnEntry() {
        CombatAffinityTables tables = CombatAffinityReader.Read(BuildExe());

        Assert.Equal(CombatAffinityTables.CreatureClassCount, tables.Creatures.Count);
        for (var i = 0; i < tables.Creatures.Count; i++) {
            Assert.Equal(i, tables.Creatures[i].ClassId);
        }
    }

    [Fact]
    public void WeaknessAndResistanceAreReadFromTheirOwnTables() {
        var weakness = new int[CombatAffinityTables.CreatureClassCount];
        var resistance = new int[CombatAffinityTables.CreatureClassCount];
        weakness[19] = 0x0001;
        resistance[22] = 0x00c0;

        CombatAffinityTables tables = CombatAffinityReader.Read(BuildExe(weakness: weakness, resistance: resistance));

        Assert.Equal(0x0001, tables.Creatures[19].WeaknessFlags);
        Assert.Equal(0, tables.Creatures[19].ResistanceFlags);
        Assert.Equal(0x00c0, tables.Creatures[22].ResistanceFlags);
        Assert.Equal(0, tables.Creatures[22].WeaknessFlags);
    }

    [Fact]
    public void AClassWithNeitherIsReportedPlain() {
        CombatAffinityTables tables = CombatAffinityReader.Read(BuildExe());

        Assert.True(tables.Creatures[0].IsPlain);
    }

    [Fact]
    public void TheThresholdTablesAreReadFromTheirOwnAddresses() {
        var exe = new byte[0x40000];
        exe[0] = (byte)'M';
        exe[1] = (byte)'Z';
        exe[8] = HeaderParagraphs & 0xff;
        exe[9] = (HeaderParagraphs >> 8) & 0xff;
        Write(exe, Offset(CombatAffinityReader.ClassGroupModifierAddress), Modifiers);
        Write(exe, Offset(CombatAffinityReader.StatCheckThresholdAddress),
            new short[] { 10, 10, 10, 0, 0, 0, 0, 0, 0 });
        Write(exe, Offset(CombatAffinityReader.AiFleeThresholdAddress),
            new short[] { 85, 55, 45, 35, 25, 20, 10, 5, 5, 0 });

        CombatAffinityTables tables = CombatAffinityReader.Read(exe);

        Assert.Equal(new[] { 10, 10, 10, 0, 0, 0, 0, 0, 0 }, tables.StatCheckThresholds);
        Assert.Equal(new[] { 85, 55, 45, 35, 25, 20, 10, 5, 5, 0 }, tables.AiFleeThresholds);
    }

    [Fact]
    public void TheTwoThresholdTablesAreAdjacentWhichCrossChecksBoth() {
        // 9 shorts at 0x3B246 ends exactly where 0x3B258 begins. If either address were wrong the
        // arrays would overlap or leave a hole, so this is a cheap guard on both at once.
        Assert.Equal(CombatAffinityReader.AiFleeThresholdAddress,
            CombatAffinityReader.StatCheckThresholdAddress
            + (CombatAffinityReader.StatCheckThresholdCount * 2));
    }
}
