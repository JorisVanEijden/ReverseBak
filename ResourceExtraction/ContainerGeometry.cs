namespace ResourceExtraction;

using GameData.Resources.Data;
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
