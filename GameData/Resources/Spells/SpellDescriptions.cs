namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// SPELLDOC.DAT — the indexed spell-description string table shown in the spell UI
/// (per-spell name / cost / duration / effect lines). Reversed from the second half of
/// <c>Load_spells</c> (ovr173 @ 0x66700).
///
/// On-disk layout:
///   u16 count                     (315)
///   u32 offset[count]             // byte offsets into the string blob
///   u16 declaredSize              // = the whole file size in shipped data (see note)
///   byte[...] blob                // NUL-terminated strings, runs to EOF
///
/// The loader fixes each offset up by adding the blob base, then reads the strings. The
/// <c>declaredSize</c> word in the shipped file is the full file size (4003), not the blob
/// length: the game over-allocates that many bytes and the file read simply stops at EOF,
/// so the real blob is everything after the header (2739 bytes). The extractor reads the
/// blob to EOF and resolves each offset.
///
/// Entries are grouped per spell (name, then cost / duration / effect lines, with blank
/// separator entries that point at the shared empty string). See
/// <c>docs/FileFormats/SPELLDOC.DAT.md</c>.
/// </summary>
public class SpellDescriptions : IResource {
    public SpellDescriptions(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Description strings in file order (index = the entry's offset-table slot).
    /// Many entries share the same offset (e.g. the empty separator string).</summary>
    public List<string> Descriptions { get; set; } = new();
}
