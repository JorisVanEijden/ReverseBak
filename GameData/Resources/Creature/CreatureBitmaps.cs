namespace GameData.Resources.Creature;

using System.Collections.Generic;

/// <summary>Engine-independent creature→sprite binding extracted from BNAMES.DAT.
///
/// <para>The original game's per-creature <c>colorSet</c> (a CSx.DAT palette-remap selector — an
/// original-binary detail) is <b>consumed by the extractor and never stored here</b>: it is baked into
/// the opaque <see cref="CreatureSprite.SpriteKeys"/> instead. A consumer only ever sees "creature N uses
/// these sprites" and never needs to know the colorset mechanism exists.</para></summary>
public class CreatureBitmaps(string id) : IResource {
    public ResourceType Type => ResourceType.CREATURE;
    public string Id { get; } = id;
    public List<CreatureSprite> Creatures { get; set; } = [];
}

public class CreatureSprite {
    /// <summary>BNAMES.DAT slot index (the creature number the game indexes by).</summary>
    public int CreatureId { get; set; }

    /// <summary>Short creature code from BNAMES (e.g. "mor", "gnt") — a domain label, not a colorset detail.</summary>
    public string Name { get; set; } = "";

    /// <summary>The creature's bitmap-set keys (one per BNAMES bitmap number). Opaque identifiers; for a
    /// recolored creature they carry a <c>_CS&lt;n&gt;</c> suffix that only the extraction layer parses
    /// (e.g. <c>GNT1_CS4</c>). Frames load as <c>&lt;key&gt;#k</c>; overrides are <c>&lt;key&gt;/k.png</c>.</summary>
    public List<string> SpriteKeys { get; set; } = [];
}
