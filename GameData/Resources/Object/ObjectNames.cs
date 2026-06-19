namespace GameData.Resources.Object;

using GameData.Resources;
using System.Collections.Generic;

/// <summary>
/// Engine-independent view of ONAMES.DAT — the indexed object *name* table.
///
/// A distinct name list from the names embedded in OBJINFO.DAT: it uses the
/// game's all-caps-keyword presentation (e.g. "CRYSTAL Staff", "Scroll MAD GODS
/// RAGE") and includes ~34 quest items / containers / trap states that OBJINFO
/// has no entry for (verified by byte comparison 2026-06-19 — only 4 of 73 names
/// are byte-identical to OBJINFO).
///
/// NOTE: ONAMES.DAT is an authoring/build-time source, NOT loaded by the shipped
/// game (no filename string in the image in any case; the runtime object table is
/// OBJINFO.DAT). It is shipped in KRONDOR.001 but never opened at runtime. This
/// extractor captures it as canonical original object-name data for the port.
///
/// Format: u16 count; (count+1) u16 offsets relative to the string-data base
/// (= 2 + 2*(count+1)); NUL-terminated strings. The trailing (count+1)th offset
/// is an end sentinel. <see cref="Names"/> is indexed 0..count-1.
/// </summary>
public class ObjectNames : IResource {
    public ObjectNames(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    public List<string> Names { get; set; } = new();
}
