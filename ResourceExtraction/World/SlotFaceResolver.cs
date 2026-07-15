namespace ResourceExtraction.World;

using GameData.Resources.World;

/// <summary>Decides whether a world polygon face is slot-textured and which bitmap it uses.
/// Confirmed rule (2026-07-15): textured iff (Flags & 0x10) and the face is a quad (4 verts);
/// the bitmap is the global index VgaColor into the zone's concatenated slot bitmaps.
/// Flat/terrain faces (0x10 clear, or non-quad) return null — caller keeps current behavior.</summary>
public static class SlotFaceResolver {
    private const byte TexturedFlag = 0x10;

    public static SlotBitmapRef? Resolve(byte flags, byte vgaColor, int vertexCount, ZoneSlotBitmapIndex index) {
        if ((flags & TexturedFlag) == 0) return null;   // flat / terrain-strip path
        if (vertexCount != 4) return null;               // original only textures quads (drawShadedQuad)
        return index.TryResolve(vgaColor, out var r) ? r : (SlotBitmapRef?)null;
    }
}
