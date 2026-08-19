namespace GameData.Resources.World;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// A zone's <c>Z##.DAT</c> — the two pens the sky and ground bands are filled with, and the pen
/// remap the overhead map is drawn through. Twenty bytes in most zones.
/// </summary>
/// <remarks>
/// Read by <c>ResourceLoad_Z__.DAT</c> (IDA 0x6d3a0) as part of the zone load, and consumed in two
/// places: the sky/ground band setup (<c>sub_ovr138_5B5</c> → <c>drawHorizonSkyAndGround</c>) takes
/// the pens, and the overhead map installs <see cref="Remaps"/> into the render view for as long
/// as it is up.
///
/// <para><b>Not to be confused with <c>Z##DEF.DAT</c></b> (<see cref="ZoneDefinition"/>), which
/// carries its own SkyColor/GroundColor bytes. Those are the <i>flat</i> fills a zone whose flags
/// say "no horizon" / "flat ground" uses instead; these are the ones every other zone uses.</para>
/// </remarks>
public class ZoneAppearance : IResource {
    public ZoneAppearance(string id) { Id = id; }

    /// <summary>Entries in the remap — one per pen.</summary>
    public const int PenCount = 256;

    /// <summary>
    /// The pen filled <b>above</b> the horizon line.
    /// </summary>
    /// <remarks>
    /// Only reached when the zone does not carry <see cref="ZoneFlags.NoHorizon"/>; that flag makes
    /// the band use <see cref="ZoneDefinition.SkyColor"/> instead. In a zone with the bitmap
    /// panorama this is what shows above the mountain strip.
    /// </remarks>
    public int SkyPen { get; set; }

    /// <summary>
    /// The pen on the other side of the horizon — and <b>what the overhead map fills its whole
    /// viewport with</b>.
    /// </summary>
    /// <remarks>
    /// The sky renderer swaps this with <see cref="SkyPen"/> when the view passes the horizon, so
    /// the two are one pair: whichever band is below gets this one. That the overhead map fills
    /// with it follows — looking straight down, everything is ground.
    /// </remarks>
    public int GroundPen { get; set; }

    /// <summary>
    /// The third value, which nothing reads.
    /// </summary>
    /// <remarks>
    /// It is passed to the band setup with the other two and stored (<c>g_bCurSkyColorB</c>), but no
    /// code reads it back; the overhead map's box fill also copies it into the draw pattern byte. It
    /// is <b>0 in every shipped zone</b>, so no behaviour has ever depended on it — kept because
    /// leaving a field out of a format is how the next reader ends up mis-aligned.
    /// </remarks>
    public int UnusedPen { get; set; }

    /// <summary>
    /// The pens the overhead map draws differently, as (pen, replacement) — every pen not listed
    /// is drawn as itself.
    /// </summary>
    /// <remarks>
    /// The changes rather than a 256-entry table, because that is what the file stores and what a
    /// modder would want to author: the original builds an identity table and overwrites these.
    /// This is the whole reason the overhead map looks like a map rather than like the world from
    /// above — the terrain pens are swapped for flatter ones.
    ///
    /// <para>The underground zones ship an <b>empty</b> list. They never reach this render — the
    /// overhead map draws the dungeon automap there instead.</para>
    /// </remarks>
    public PenRemap[] Remaps { get; set; } = Array.Empty<PenRemap>();

    /// <summary>How many pens the remap changes — pairs that map a pen to itself do not count.</summary>
    [JsonIgnore]
    public int RemappedPenCount {
        get {
            var changed = 0;
            foreach (PenRemap remap in Remaps) {
                if (remap.DrawnAs != remap.Pen) {
                    changed++;
                }
            }

            return changed;
        }
    }

    /// <summary>The 256-entry table the renderer wants: identity, with the changes applied.</summary>
    public byte[] ToPenTable() {
        var table = new byte[PenCount];
        for (var i = 0; i < PenCount; i++) {
            table[i] = (byte)i;
        }

        foreach (PenRemap remap in Remaps) {
            if (remap.Pen >= 0 && remap.Pen < PenCount) {
                table[remap.Pen] = (byte)remap.DrawnAs;
            }
        }

        return table;
    }

    /// <summary>The pen <paramref name="pen"/> is drawn as while the overhead map is up.</summary>
    public int MapPenFor(int pen) {
        if (pen < 0 || pen >= PenCount) {
            throw new ArgumentOutOfRangeException(nameof(pen));
        }

        foreach (PenRemap remap in Remaps) {
            if (remap.Pen == pen) {
                return remap.DrawnAs;
            }
        }

        return pen;
    }

    public ResourceType Type => ResourceType.DAT;

    public string Id { get; }
}

/// <summary>One pen the overhead map draws as another.</summary>
public class PenRemap {
    /// <summary>The pen as the world data has it.</summary>
    public int Pen { get; set; }

    /// <summary>The pen it is drawn as on the map.</summary>
    public int DrawnAs { get; set; }
}
