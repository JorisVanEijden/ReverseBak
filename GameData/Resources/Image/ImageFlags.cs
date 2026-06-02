namespace GameData.Resources.Image;

/// <summary>
/// Per-image flags from a "Normal" (sig 0x1066) BMX image-directory entry.
///
/// <para><b>Engine usage (investigated 2026-06-02 from KRONDOR.EXE — instruction-level sweep).</b>
/// Reading the actual code, not just field-xrefs (which miss raw <c>[reg+4]</c> access): the loader
/// <c>LoadBMPorBMX?</c> (0x1628e) stores the file's flags u16 <i>verbatim</i> into <c>bitmap.flags</c>
/// (+4) and branches on none of the per-image bits (only the bulk-header compression). To catch
/// untyped reads, every one of the 47 functions that touch a bitmap struct was scanned at the
/// instruction level for <c>[reg+4]</c> memory operands, and the base register of each hit was traced:
/// the only ones where the base actually holds a bitmap pointer are the four <c>flags</c> reads in
/// <c>sub_seg017_132E</c> (0x17f7e), <c>ApplyColorSetToBitmap</c> (0x1657c) and <c>anim_handle_drawing</c>
/// (0x52955) — all testing only <see cref="ReversedRowColumn"/> (0x20) and <see cref="Compressed"/>
/// (0x80). Every other <c>[reg+4]</c> read targets a non-bitmap structure (mesh-face records, vertices,
/// a clip rect, stack scratch, animation frame-command data). The bitmap pointer itself is only ever
/// dereferenced at +0/+2 (data), +6 (Width), +8 (Height) — never +4. Sprite flip comes from negative
/// width/height arguments at call time, not stored 0x01/0x02. So 0x01/0x02/0x04/0x40 are authoring
/// metadata, safe to ignore for rendering.
/// <b>Call-graph sweep (2026-06-02) — strong evidence, NOT proof.</b> Scanning the bitmap-typed
/// functions plus all callers of the known blitters/getters/loader/cache (63 funcs) for <c>[reg+4]</c>
/// reads found only the four typed readers above touching a bitmap's flags (all testing 0x20/0x80);
/// every other +4 read traced to a non-bitmap struct (vertices, mesh records, dialog-action structs,
/// UI rects, config). <b>Caveat:</b> KRONDOR.EXE is far from fully typed/reversed (~46% still sub_*),
/// so a bitmap handler whose pointer/locals aren't typed as <c>bitmap</c> and that doesn't route through
/// a known blitter (a custom inline blit, or an un-RE'd helper that inspects flags) would fall outside
/// this sweep. So 0x04/0x40 are "no consumer found, very likely unused" — not proven unused.</para>
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

    /// <summary>0x04. High bit of a 0–7 subfield (0x01/0x02/0x04) carried only by compressed
    /// column-major images in the inventory/icon sheets (INVSHP1/2, BICONS1/2, INVMISC, INVLOCK,
    /// TELEPORT). Not read by the render engine. Content analysis (2026-06-02) rules out a
    /// size/grid-footprint meaning (each value spans wildly different dimensions). Correlating each
    /// item's icon to its subfield — via the real mapping <c>icon!=0 ? icon : objectNumber</c>
    /// (getIconImageData 0x56185; objinfo `icon=0` is a sentinel, NOT a bug) — also shows the 0–7
    /// value does <b>not</b> encode ObjectType, InventorySlots, or image size; sheet-position/art-batch
    /// and compressed-size show no correlation either (values scattered, avg run ~1.3 in INVSHP1).
    /// These are negative results against the data I could join — <b>not</b> a closed answer: the
    /// inventory/icon code is largely un-reversed, so the meaning may well be recoverable by reversing
    /// those paths (or from the original art tool/format). Current status: undetermined per-item
    /// authoring metadata, no data-side correlation found, apparently not engine-read (same sweep
    /// caveat as the type summary).</summary>
    Unknown4 = 0x04,

    /// <summary>0x08. Never set in any shipped image — dead bit.</summary>
    Unknown8 = 0x08,

    /// <summary>0x10. Never set in any shipped image — dead bit.</summary>
    Unknown16 = 0x10,

    /// <summary>0x20. Image stored column-major; read at load and by anim_handle_drawing (0x52ab4)
    /// to pick the column-aware blit path.</summary>
    ReversedRowColumn = 0x20,

    /// <summary>0x40. "Sprite-like content" marker (~90% of images). Content analysis of all 4244
    /// decoded images (2026-06-02) shows <c>0x40 ⇔ (image is per-image RLE-compressed OR contains
    /// index-0/transparent pixels)</c> — a 99.4% match (4219/4244). The 25 exceptions are 19
    /// BOOK.BMX page-art images (transparent yet authored as 0x00) and 6 tiny fully-opaque compressed
    /// icons. So 0x40 distinguishes transparent/compressed sprites from plain opaque rectangular
    /// fills/strips (the ~393 no-0x40 opaque images). Not read by the render engine.</summary>
    Unknown64 = 0x40,

    /// <summary>0x80. Per-image RLE applied AFTER the bulk decompress. Read at load (decompress)
    /// and in the EGA blitter (0x18006).</summary>
    Compressed = 0x80
}
