namespace GameData.Resources.Image;

/// <summary>
/// Per-image flags from a "Normal" (sig 0x1066) BMX image-directory entry.
///
/// <para><b>Engine usage (verified 2026-06-02 from KRONDOR.EXE):</b> the only bits the runtime ever
/// reads from a loaded image's stored flags are <see cref="ReversedRowColumn"/> (0x20) and
/// <see cref="Compressed"/> (0x80). Every consumer of the in-memory <c>bitmap.flags</c> field —
/// the EGA planar blitter (sub_seg017_132E 0x17f7e), ApplyColorSetToBitmap (0x1657c) and
/// anim_handle_drawing (0x52955) — tests only those two. So 0x01/0x02/0x04/0x40 are <b>not</b>
/// consumed by rendering; they are authoring metadata and are safe to ignore in the port.</para>
///
/// <para><b>Shipped-data distribution (4225 images across 413 files):</b> 0x08 and 0x10 are
/// <b>never set</b>. 0x40 is set on 90.6% of images. 0x01/0x02/0x04 only ever appear together with
/// 0xE0 (column-major + 0x40 + compressed) and <b>only in inventory/icon sheets</b>
/// (INVSHP1/2, BICONS1/2, INVMISC, INVLOCK, TELEPORT).</para>
/// </summary>
[Flags]
public enum ImageFlags {
    /// <summary>0x01. <b>Runtime (blitter) meaning:</b> vertical flip — the blitter sets this by
    /// XOR when called with negative height (sub_seg017_132E 0x17fa2), it is not driven by the
    /// stored bit. <b>Stored meaning:</b> low bit of a 0–7 subfield seen only on inventory/icon
    /// sprites (likely item display metadata); not read by the engine.</summary>
    VerticalFlip = 0x01,

    /// <summary>0x02. <b>Runtime (blitter) meaning:</b> horizontal flip — set by the blitter from a
    /// negative width (0x17f94). <b>Stored meaning:</b> middle bit of the inventory/icon 0–7
    /// subfield; not read by the engine.</summary>
    HorizontalFlip = 0x02,

    /// <summary>0x04. High bit of the inventory/icon 0–7 subfield (212 images, all compressed
    /// column-major, all in INV*/BICONS*/TELEPORT sheets). Not read by the render engine; likely
    /// an inventory display/shape hint. Unverified beyond "non-rendering authoring metadata".</summary>
    Unknown4 = 0x04,

    /// <summary>0x08. Never set in any shipped image — dead bit.</summary>
    Unknown8 = 0x08,

    /// <summary>0x10. Never set in any shipped image — dead bit.</summary>
    Unknown16 = 0x10,

    /// <summary>0x20. Image stored column-major; read at load and by anim_handle_drawing (0x52ab4)
    /// to pick the column-aware blit path.</summary>
    ReversedRowColumn = 0x20,

    /// <summary>0x40. Content marker, set on ~90% of images. Present on essentially every
    /// per-image-compressed sprite (3101/3107; the only 6 exceptions are icon images in
    /// BICONS1/2 + INVMISC) plus 725 uncompressed sprites; absent on short opaque strips/fills
    /// (avg height ~14px). Not read by the render engine — most consistent with "non-trivial sprite
    /// content / has transparency", but its exact authoring meaning is unconfirmed.</summary>
    Unknown64 = 0x40,

    /// <summary>0x80. Per-image RLE applied AFTER the bulk decompress. Read at load (decompress)
    /// and in the EGA blitter (0x18006).</summary>
    Compressed = 0x80
}
