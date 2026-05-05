namespace GameData.Resources.Data;

using System.Collections.Generic;

// Generic IResource wrapper for one DEF_*.DAT file. Holds the list of
// per-format entries parsed by a DefFamilyExtractorBase<TEntry> subclass.
// See docs/FileFormats/DEF_DAT family.md.
public class DefFamilyFile<TEntry> : IResource {
    public DefFamilyFile(string id, List<TEntry> entries) {
        Id = id;
        Entries = entries;
    }

    public ResourceType Type => ResourceType.DEF;
    public string Id { get; }
    public List<TEntry> Entries { get; }
}
