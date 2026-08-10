namespace ResourceExtraction;

using GameData.Resources.Data;
using GameData.Resources.Inventory;
using System;
using System.Collections.Generic;

/// <summary>
/// Byte geometry of the save's zone-container section (reverse of SaveGameExtractor.ParseContainer).
/// A container's serialized size depends only on capacity + dataTypes (the item array is always
/// `capacity` 4-byte slots), so it is invariant to looting — enabling in-place write-back patches.
/// </summary>
public static class ContainerGeometry {
    // = StateData 2775 + World 34320 + Actor 164350 + Combat 38060.
    public const int ZoneContainerSectionStart = 239505;
    public const int HeaderSize = 16;
    public const int ItemSize = 4;
    public const int NumberOfItemsOffset = 13; // within the container
    public const int ItemArrayOffset = 16;     // within the container

    /// <summary>
    /// Pack a container's 16-byte header — the exact reverse of SaveGameExtractor.ParseContainer's
    /// prologue: zone, packed chapter band, world item id, x, y, residence, item count, capacity,
    /// flags. The actor number is not written separately because the reader aliases it onto the low
    /// word of X.
    /// </summary>
    public static byte[] PackHeader(RuntimeContainer container) {
        var header = new byte[HeaderSize];
        header[0] = (byte)container.Zone;
        header[1] = (byte)(((container.MinChapter & 0x0F) << 4) | (container.MaxChapter & 0x0F));
        BitConverter.GetBytes(container.WorldItemId).CopyTo(header, 2);
        BitConverter.GetBytes(container.X).CopyTo(header, 4);
        BitConverter.GetBytes(container.Y).CopyTo(header, 8);
        header[12] = (byte)container.ContainerType;
        header[NumberOfItemsOffset] = (byte)container.Items.Count;
        header[14] = (byte)container.Capacity;
        header[15] = (byte)container.DataTypes;
        return header;
    }

    /// <summary>
    /// Offset of the SUBREC_LAST_TOUCH timestamp within a container, or -1 when the record
    /// carries no <see cref="SaveGameContainerDataType.Timestamp"/> subrecord. Subrecords follow
    /// the item array in ascending bit order, so this is the item array plus every lower-bit
    /// subrecord — mirroring the order SaveGameExtractor.ParseContainer reads them in.
    /// </summary>
    public static int TimestampOffset(int capacity, SaveGameContainerDataType dataTypes) {
        if ((dataTypes & SaveGameContainerDataType.Timestamp) == 0) {
            return -1;
        }
        int off = ItemArrayOffset + capacity * ItemSize;
        if ((dataTypes & SaveGameContainerDataType.Lock) != 0)      off += 4;
        if ((dataTypes & SaveGameContainerDataType.Dialog) != 0)    off += 6;
        if ((dataTypes & SaveGameContainerDataType.Shop) != 0)      off += 16;
        if ((dataTypes & SaveGameContainerDataType.Encounter) != 0) off += 9;
        return off;
    }

    public static int SerializedSize(int capacity, SaveGameContainerDataType dataTypes) {
        int size = HeaderSize + capacity * ItemSize;
        if ((dataTypes & SaveGameContainerDataType.Lock) != 0)        size += 4;
        if ((dataTypes & SaveGameContainerDataType.Dialog) != 0)      size += 6;
        if ((dataTypes & SaveGameContainerDataType.Shop) != 0)        size += 16;
        if ((dataTypes & SaveGameContainerDataType.Encounter) != 0)   size += 9;
        if ((dataTypes & SaveGameContainerDataType.Timestamp) != 0)   size += 4;
        if ((dataTypes & SaveGameContainerDataType.GlobalState) != 0) size += 2;
        return size;
    }

    /// <summary>Absolute body offset of container `index` within a zone whose section-local
    /// start is `zoneLocalOffset` (containers preceded by a 2-byte count).</summary>
    public static int ContainerBodyOffset(int zoneLocalOffset, IReadOnlyList<SaveGameContainerData> zoneContainers, int index) {
        int off = ZoneContainerSectionStart + zoneLocalOffset + 2;
        for (int i = 0; i < index; i++) {
            off += SerializedSize(zoneContainers[i].Capacity, zoneContainers[i].DataTypes);
        }
        return off;
    }
}
