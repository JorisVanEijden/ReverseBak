namespace GameData.Resources.Data;

/// <summary>A changed container's bytes to patch in place.
///
/// <para>Looting alone only moves items, so the common case is <see cref="NumberOfItems"/> plus
/// the live item array — everything else in the record is byte-unchanged. Claiming or releasing a
/// ground bag also rewrites the 16-byte header (zone, chapter band, world item id, x/y, residence)
/// and the last-touch timestamp, which <see cref="HeaderBytes"/> and <see cref="TimestampOffset"/>
/// carry. The record's serialized size never changes either way — capacity and the subrecord mask
/// are immutable — so the patch stays exact.</para>
/// </summary>
public sealed class DirtyContainerEdit {
    public int BodyOffset;       // absolute offset in the save BODY of this container's header
    public byte NumberOfItems;   // new live item count
    public byte[] LiveItemBytes; // NumberOfItems*4 bytes: each item = [ObjectId, Variable, ItemFlags&0xFF, ItemFlags>>8]

    /// <summary>The full 16-byte header to write at <see cref="BodyOffset"/>, or null to leave the
    /// header alone (and patch only <see cref="NumberOfItems"/> + the items, as looting does). The
    /// writer applies it before <see cref="NumberOfItems"/>, so that field stays authoritative
    /// whichever value the header block happens to carry in its count byte.</summary>
    public byte[] HeaderBytes;

    /// <summary>Offset of the last-touch timestamp relative to <see cref="BodyOffset"/>, or -1
    /// when the record has no timestamp subrecord or it did not change.</summary>
    public int TimestampOffset = -1;

    /// <summary>The last-touch game time to write, valid when <see cref="TimestampOffset"/> is
    /// non-negative.</summary>
    public int Timestamp;

    /// <summary>Offset of the shop sub-record relative to <see cref="BodyOffset"/>, or -1 when the
    /// record carries none.</summary>
    /// <remarks>
    /// The block holds things gameplay SPENDS — a tavern's entertainment fund is zeroed by the one
    /// performance it pays for — so a container that never wrote it back would pay again on the
    /// next visit.
    /// </remarks>
    public int ShopOffset = -1;

    /// <summary>The sixteen shop bytes, valid when <see cref="ShopOffset"/> is non-negative.</summary>
    public byte[] ShopBytes;
}
