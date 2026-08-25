namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.GameState;
using ResourceExtraction;
using System.Collections.Generic;
using System.Text;
using Xunit;

/// <summary>
/// The story flags reaching the save — the hole TASK-210 filed: nothing wrote these at all, so
/// every flag a dialog set lasted only until you saved.
/// </summary>
public class GlobalFlagPersistenceTests {
    // CP437 for the slot name, like every other writer fixture here.
    static GlobalFlagPersistenceTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] Body() => new byte[SaveGameOffsets.BodySize];

    private static SaveGameFieldsHolder Fields(byte[] body) => new SaveGameFieldsHolder(body);

    /// <summary>Minimal fields; the flag block is what is under test.</summary>
    private sealed class SaveGameFieldsHolder {
        public readonly GameData.Resources.Data.SaveGameFields Value;
        public SaveGameFieldsHolder(byte[] body) {
            Value = new GameData.Resources.Data.SaveGameFields(
                Chapter: 1, PartyGold: 0, GameTime: 0, TimeSnapshot: 0, PaletteEventMask: 0,
                PartyDeathState: 0, ChapterTransitionPending: 0, PreviousZone: 0, CurrentZone: 1,
                WorldX: 0, WorldY: 0, PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0);
        }
    }

    private static byte[] WriteWithFlags(byte[] body, Dictionary<int, int> flags) {
        SaveGameWriteResult r = SaveGameWriter.Write(
            body, Fields(body).Value, "Slot A", 0, 0, 0, globalFlagEdits: flags);
        return r.Bytes[SaveGameOffsets.HeaderSize..];
    }

    [Fact]
    public void ALowFlagReachesTheLowBitmapAtTheBitTheOriginalUses() {
        // 8127 is the corpse-flavour flag the dialog corpus actually sets.
        byte[] outBody = WriteWithFlags(Body(), new Dictionary<int, int> { [8127] = 1 });

        Assert.True(GlobalFlagLayout.TryLowPosition(8127, out int index, out int bit));
        Assert.Equal(1, (outBody[SaveGameOffsets.GlobalFlags + index] >> bit) & 1);
    }

    [Fact]
    public void AHighFlagReachesTheTENPERBYTEPosition_notALinearOne() {
        // *** The whole point of TASK-209. *** A linear writer would put 56013 at bit 13 of the
        // block (byte 1, bit 5); the original puts it at row 1, bit 2.
        byte[] outBody = WriteWithFlags(Body(), new Dictionary<int, int> { [56013] = 1 });

        Assert.True(GlobalFlagLayout.TryHighPosition(56013, out int row, out int bit));
        Assert.Equal(1, row);
        Assert.Equal(2, bit);
        Assert.Equal(1, (outBody[SaveGameOffsets.GlobalFlags2 + row] >> bit) & 1);

        int linearByte = (56013 - 56000) / 8, linearBit = (56013 - 56000) % 8;
        Assert.NotEqual((linearByte, linearBit), (row, bit));
        Assert.Equal(0, (outBody[SaveGameOffsets.GlobalFlags2 + linearByte] >> linearBit) & 1);
    }

    [Fact]
    public void EditsAreAppliedONTOTheExistingState_notInsteadOfIt() {
        // The overlay holds only what changed this session. Replacing the block instead would wipe
        // every flag the loaded save carried.
        byte[] body = Body();
        Assert.True(GlobalFlagLayout.TryLowPosition(4001, out int keepIndex, out int keepBit));
        body[SaveGameOffsets.GlobalFlags + keepIndex] |= (byte)(1 << keepBit);

        byte[] outBody = WriteWithFlags(body, new Dictionary<int, int> { [8127] = 1 });

        Assert.Equal(1, (outBody[SaveGameOffsets.GlobalFlags + keepIndex] >> keepBit) & 1);
    }

    [Fact]
    public void ClearingAFlagClearsIt() {
        byte[] body = Body();
        Assert.True(GlobalFlagLayout.TryLowPosition(8127, out int index, out int bit));
        body[SaveGameOffsets.GlobalFlags + index] |= (byte)(1 << bit);

        byte[] outBody = WriteWithFlags(body, new Dictionary<int, int> { [8127] = 0 });

        Assert.Equal(0, (outBody[SaveGameOffsets.GlobalFlags + index] >> bit) & 1);
    }

    [Fact]
    public void NoEditsTouchesNOTHING_andClaimsNoCoverage() {
        // A save taken with no flag changes must not rewrite the block, and must not report those
        // 1113 bytes as authored when it did not author them.
        byte[] body = Body();
        body[SaveGameOffsets.GlobalFlags] = 0xAB;

        SaveGameWriteResult r = SaveGameWriter.Write(body, Fields(body).Value, "Slot A", 0, 0, 0);

        Assert.Equal(0xAB, r.Bytes[SaveGameOffsets.HeaderSize + SaveGameOffsets.GlobalFlags]);
        foreach ((int offset, int length) in r.Coverage.AuthoredRanges) {
            Assert.False(offset <= SaveGameOffsets.GlobalFlags
                && SaveGameOffsets.GlobalFlags < offset + length);
        }
    }

    [Fact]
    public void TheTwoBlocksSitWhereTheStructSaysAndFillItToTheLastByte() {
        // Derived by summing gstate.inc, cross-checked four ways (see SaveGameOffsets.GlobalFlags).
        Assert.Equal(1662, SaveGameOffsets.GlobalFlags);
        Assert.Equal(2725, SaveGameOffsets.GlobalFlags2);
        Assert.Equal(SaveGameOffsets.StateDataSize,
            SaveGameOffsets.GlobalFlags2 + SaveGameOffsets.GlobalFlags2Size);
        Assert.Equal(SaveGameOffsets.GlobalFlags2,
            SaveGameOffsets.GlobalFlags + SaveGameOffsets.GlobalFlagsSize);
    }
}
