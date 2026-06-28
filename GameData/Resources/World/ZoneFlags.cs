namespace GameData.Resources.World;

using System;

/// <summary>
/// Per-zone rendering flags: the 5th field of <c>Z##DEF.DAT</c> (offset 0x0A, u16), held at runtime
/// in the engine global <c>word_dseg_1E76</c>. Reverse-engineered from KRONDOR.EXE (2026-06-28).
///
/// These control how the distant background (sky / horizon / ground) and the zone's open edges are
/// drawn — independent of <see cref="ZoneDefinition.ZoneLocation"/> (above/underground). Shipped
/// zones only ever use bits 0–2 (max value 0x07).
/// </summary>
[Flags]
public enum ZoneFlags : ushort {
    None = 0,

    /// <summary>
    /// Bit 0 — no painted horizon backdrop. The engine skips loading/drawing <c>Z##H.BMX</c> and fills
    /// the sky band above the horizon line with the flat <see cref="ZoneDefinition.SkyColor"/>. Set for
    /// exactly the zones that ship without a <c>Z##H.BMX</c> (Z08–Z12); clear for Z01–Z07.
    /// (load gate: resource_loadZoneDataFiles @0x733b8; render: drawHorizonSkyAndGround @0x46176/0x461e5)
    /// </summary>
    NoHorizon = 0x0001,

    /// <summary>
    /// Bit 1 — flat ground. Fill the ground band below the horizon with the flat
    /// <see cref="ZoneDefinition.GroundColor"/> instead of the textured/striped ground fill.
    /// (drawHorizonSkyAndGround @0x461b6 → groundColor vs drawGroundStripFill)
    /// </summary>
    FlatGround = 0x0002,

    /// <summary>
    /// Bit 2 — close the zone's open edges with boundary objects. Tiles just outside the loaded zone
    /// map are recorded (UpdateWorldItemsForTile @0x72d54, capped at 9) and rendered as world-object
    /// type 181 around the visible perimeter (sub_seg021_231 @0x219e3), so the world doesn't end at an
    /// open void.
    /// </summary>
    DrawEdgeObjects = 0x0004,

    /// <summary>
    /// Bit 3 — ground fill spans to the bottom of the viewport instead of a fixed 12 rows. NOT authored
    /// in zone data (always clear in the shipped <c>Z##DEF.DAT</c> files): the engine ORs it in
    /// transiently while the rotate/look view is active (sub_ovr161_1061 @0x59c23) and clears it on
    /// exit; drawGroundStripFill @0x22bc6 reads it. Documented for completeness of the flags word.
    /// </summary>
    GroundFillToViewportBottom = 0x0008,
}
