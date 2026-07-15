namespace ResourceExtraction.World;

using System.Collections.Generic;
using GameData.Resources.World;

/// <summary>Maps a face's global slot-bitmap index (VgaColor on a Flags&0x10 face) to a
/// (slotFile, localImage) reference, using the zone's per-file slot image counts. The zone's
/// Z##SLOT#.BMX files are concatenated in file order; global 0 = SLOT0 image 0.
/// Confirmed 2026-07-15 (chest→SLOT3, bridge→SLOT2 across all Z01 Flags&0x10 faces).</summary>
public sealed class ZoneSlotBitmapIndex {
    private readonly IReadOnlyList<int> _counts;
    private readonly int _total;

    public ZoneSlotBitmapIndex(IReadOnlyList<int> slotImageCounts) {
        _counts = slotImageCounts;
        int t = 0;
        foreach (int c in slotImageCounts) t += c;
        _total = t;
    }

    public bool TryResolve(int globalIndex, out SlotBitmapRef bitmapRef) {
        bitmapRef = default;
        if (globalIndex < 0 || globalIndex >= _total) return false;
        int remaining = globalIndex;
        for (int slot = 0; slot < _counts.Count; slot++) {
            if (remaining < _counts[slot]) { bitmapRef = new SlotBitmapRef(slot, remaining); return true; }
            remaining -= _counts[slot];
        }
        return false;
    }
}
