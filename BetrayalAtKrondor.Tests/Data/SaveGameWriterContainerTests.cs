namespace BetrayalAtKrondor.Tests.Data;

using System;
using System.Text;
using GameData.Resources.Data;
using ResourceExtraction;
using Xunit;

public class SaveGameWriterContainerTests {
    static SaveGameWriterContainerTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static readonly SaveGameFields ZeroFields = new(
        Chapter: 0, PartyGold: 0, GameTime: 0,
        CurrentZone: 0, WorldX: 0, WorldY: 0,
        PositionX: 0, PositionY: 0, PositionZ: 0, Rotation: 0);

    // A container body offset: start of the first zone's container section, past the 2-byte count.
    private static readonly int ContainerOffset = ContainerGeometry.ZoneContainerSectionStart + 2;

    // Builds a body with a container at ContainerOffset: capacity 4, numberOfItems 2, two items,
    // and trailing (unused) item slots filled with a 0xAB sentinel so we can prove they're untouched.
    private static byte[] BodyWithContainer() {
        byte[] body = new byte[SaveGameOffsets.BodySize];
        int off = ContainerOffset;

        body[off + ContainerGeometry.NumberOfItemsOffset] = 2; // numberOfItems
        body[off + 14] = 4; // capacity (byte 14 within container header, per ContainerGeometry.HeaderSize=16)

        // Two live items at off+16 and off+20 (4 bytes each): distinct, recognisable values.
        byte[] item0 = { 0x10, 0x20, 0x30, 0x40 };
        byte[] item1 = { 0x11, 0x21, 0x31, 0x41 };
        Array.Copy(item0, 0, body, off + ContainerGeometry.ItemArrayOffset, 4);
        Array.Copy(item1, 0, body, off + ContainerGeometry.ItemArrayOffset + 4, 4);

        // Trailing slots (capacity - numberOfItems = 2 slots) filled with sentinel bytes.
        for (int i = off + ContainerGeometry.ItemArrayOffset + 8; i < off + ContainerGeometry.ItemArrayOffset + 16; i++) {
            body[i] = 0xAB;
        }
        return body;
    }

    [Fact]
    public void ContainerEdit_PatchesNumberOfItemsAndLiveItems_LeavesTrailingSlotsUntouched() {
        byte[] body = BodyWithContainer();
        byte[] newItem = { 0x99, 0x88, 0x77, 0x66 }; // different from either original item

        var edit = new DirtyContainerEdit {
            BodyOffset = ContainerOffset,
            NumberOfItems = 1,
            LiveItemBytes = newItem,
        };

        SaveGameWriteResult noEdits = SaveGameWriter.Write(body, ZeroFields, "Loot", 0, 0, 0);
        SaveGameWriteResult r = SaveGameWriter.Write(
            body, ZeroFields, "Loot", 0, 0, 0, containerEdits: new[] { edit });

        int baseOff = SaveGameOffsets.HeaderSize + ContainerOffset;

        // numberOfItems patched.
        Assert.Equal(1, r.Bytes[baseOff + ContainerGeometry.NumberOfItemsOffset]);

        // Live item (4 bytes) patched to the new item bytes.
        for (int i = 0; i < 4; i++) {
            Assert.Equal(newItem[i], r.Bytes[baseOff + ContainerGeometry.ItemArrayOffset + i]);
        }

        // Trailing slots (off+20..+31, i.e. the old item1 slot + the two sentinel slots) unchanged
        // from the original backing body.
        for (int i = baseOff + ContainerGeometry.ItemArrayOffset + 4; i < baseOff + ContainerGeometry.ItemArrayOffset + 16; i++) {
            int bodyRelative = i - SaveGameOffsets.HeaderSize;
            Assert.Equal(body[bodyRelative], r.Bytes[i]);
        }

        // Output length is unchanged vs. a no-edits write.
        Assert.Equal(noEdits.Bytes.Length, r.Bytes.Length);
    }

    /// <summary>
    /// Claiming a ground bag rewrites the record's identity, not just its items: the writer must
    /// lay the packed 16-byte header down and stamp the last-touch timestamp, while the record's
    /// length stays exactly what it was.
    /// </summary>
    [Fact]
    public void ContainerEdit_WithHeaderAndTimestamp_PatchesIdentityAndLastTouch() {
        byte[] body = BodyWithContainer();
        var claimed = new GameData.Resources.Inventory.RuntimeContainer {
            Zone = 4, MinChapter = 0, MaxChapter = 10,
            WorldItemId = GameData.Resources.Inventory.GroundContainerPool.BagWorldItemId,
            X = 0x00112233, Y = 0x00445566,
            ContainerType = SaveGameContainerType.Bag,
            Capacity = 4,
            DataTypes = SaveGameContainerDataType.Timestamp | SaveGameContainerDataType.SelfSpawn,
        };
        claimed.Items.Add(new GameData.Resources.Inventory.RuntimeItem(0x99, 0x88, 0x7766));

        int timestampOffset = ContainerGeometry.TimestampOffset(claimed.Capacity, claimed.DataTypes);
        var edit = new DirtyContainerEdit {
            BodyOffset = ContainerOffset,
            NumberOfItems = 1,
            LiveItemBytes = new byte[] { 0x99, 0x88, 0x66, 0x77 },
            HeaderBytes = ContainerGeometry.PackHeader(claimed),
            TimestampOffset = timestampOffset,
            Timestamp = 0x0A0B0C0D,
        };

        SaveGameWriteResult noEdits = SaveGameWriter.Write(body, ZeroFields, "Drop", 0, 0, 0);
        SaveGameWriteResult r = SaveGameWriter.Write(
            body, ZeroFields, "Drop", 0, 0, 0, containerEdits: new[] { edit });

        int baseOff = SaveGameOffsets.HeaderSize + ContainerOffset;

        Assert.Equal(4, r.Bytes[baseOff + 0]);                       // zone
        Assert.Equal(0x0A, r.Bytes[baseOff + 1]);                    // chapter band 0..10
        Assert.Equal(0xA6, BitConverter.ToInt16(r.Bytes, baseOff + 2));
        Assert.Equal(0x00112233, BitConverter.ToInt32(r.Bytes, baseOff + 4));
        Assert.Equal(0x00445566, BitConverter.ToInt32(r.Bytes, baseOff + 8));
        Assert.Equal((byte)SaveGameContainerType.Bag, r.Bytes[baseOff + 12]);
        Assert.Equal(1, r.Bytes[baseOff + ContainerGeometry.NumberOfItemsOffset]);
        Assert.Equal(4, r.Bytes[baseOff + 14]);                      // capacity untouched
        Assert.Equal(0x0A0B0C0D, BitConverter.ToInt32(r.Bytes, baseOff + timestampOffset));

        // The record's size is invariant, so the file length cannot move.
        Assert.Equal(noEdits.Bytes.Length, r.Bytes.Length);
    }

    [Fact]
    public void TimestampOffset_IsMinusOne_WhenTheRecordCarriesNoTimestampSubrecord() {
        Assert.Equal(-1, ContainerGeometry.TimestampOffset(20, SaveGameContainerDataType.Lock));
    }

    /// <summary>Subrecords follow the item array in ascending bit order, so a record carrying a
    /// lock and a dialog pushes its timestamp past both.</summary>
    [Fact]
    public void TimestampOffset_SkipsTheLowerBitSubrecords() {
        SaveGameContainerDataType types = SaveGameContainerDataType.Lock
            | SaveGameContainerDataType.Dialog | SaveGameContainerDataType.Timestamp;

        Assert.Equal(ContainerGeometry.ItemArrayOffset + 20 * 4 + 4 + 6,
            ContainerGeometry.TimestampOffset(20, types));
    }

    [Fact]
    public void NoContainerEdits_ProducesAByteIdenticalBodyToBaseline() {
        byte[] body = BodyWithContainer();

        SaveGameWriteResult withNullEdits = SaveGameWriter.Write(body, ZeroFields, "Loot", 0, 0, 0, containerEdits: null);
        SaveGameWriteResult withoutParam = SaveGameWriter.Write(body, ZeroFields, "Loot", 0, 0, 0);

        Assert.Equal(withoutParam.Bytes, withNullEdits.Bytes);

        // And the body portion is byte-identical to the original backing body (round-trip invariant).
        byte[] outBody = withoutParam.Bytes[SaveGameOffsets.HeaderSize..];
        Assert.Equal(body, outBody);
    }
}
