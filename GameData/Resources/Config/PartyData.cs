namespace GameData.Resources.Config;

using GameData.Resources;
using GameData.Resources.Data;
using System.Collections.Generic;

/// <summary>
/// Engine-independent view of PARTY.DAT — the six initial playable-character
/// templates the engine loads into TEMP.GAM when a new game starts (Locklear,
/// Owyn, Gorath, Pug, James, Patrus, in file order).
///
/// File layout (610 bytes): six 95-byte actor records (identical to the in-save
/// actor record — see <c>ActorRecordReader</c>), a 2-byte trailer holding the file
/// size, then the display names as NUL-terminated strings. Names are resolved by
/// each record's <c>NamePointer</c> (an offset into that trailing string block),
/// NOT positionally — the record order (Locklear, Gorath, Owyn, Pug, James, Patrus)
/// differs from the string order (Locklear, Owyn, Gorath, …).
/// </summary>
public class PartyData : IResource {
    public const int MemberCount = 6;

    public PartyData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    public List<PartyMember> Members { get; set; } = new();
}

/// <summary>One initial party character: its display name and full actor stat block.</summary>
public class PartyMember {
    public PartyMember(string name, SaveGameActorData actor) {
        Name = name;
        Actor = actor;
    }

    /// <summary>Display name resolved from the trailing string block (e.g. "Locklear").</summary>
    public string Name { get; }

    /// <summary>The decoded 95-byte actor record: 16 attributes (Health..Stealth), known spells, pointers.</summary>
    public SaveGameActorData Actor { get; }
}
