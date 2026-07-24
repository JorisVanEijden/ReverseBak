namespace GameData.Resources.Data;

using System.Collections.Generic;

// Generic IResource wrapper for one DEF_*.DAT file. Holds the list of
// per-format records parsed by a DefFamilyExtractorBase<TEntry> subclass.
// Each record carries the loader's status byte plus the per-format payload.
// See docs/FileFormats/DEF_DAT family.md.
public class DefFamilyFile<TEntry> : IResource {
    public DefFamilyFile(string id, List<DefRecord<TEntry>> records) {
        Id = id;
        Records = records;
    }

    public ResourceType Type => ResourceType.DEF;
    public string Id { get; }
    public List<DefRecord<TEntry>> Records { get; }
}

// One record from a DEF_*.DAT file. Status mirrors the on-disk byte:
//   1 = present (loader returns true, payload read)
//   0 = vacant  (loader returns false, payload bytes on disk may be stale)
// Payload is always read regardless of Status; consumers should check
// Status before treating Payload fields as meaningful.
public class DefRecord<TEntry> {
    /// <summary>Stable content-graph key: <c>base:def_&lt;family&gt;:&lt;index&gt;</c> (e.g.
    /// <c>base:def_dial:5</c>). Family segment is the DEF file's 4-letter suffix, lowercased; index is
    /// the record's positional slot (base data never reorders → stable identity). This is the
    /// de-indexed target that <c>TileEventTrigger.EntryKey</c> resolves to. See
    /// docs/re-notes/reference-inventory.md #2.</summary>
    public string Key { get; set; } = "";

    public byte Status { get; set; }
    public TEntry Payload { get; set; } = default!;
}
