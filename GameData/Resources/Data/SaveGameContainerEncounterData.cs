namespace GameData.Resources.Data;

public class SaveGameContainerEncounterData {
    public SaveGameContainerEncounterData(
        int globalDataKey1,
        int globalDataKey2,
        byte gdsNumber,
        byte gdsLetter,
        byte hasHotspot,
        byte hotspotX,
        byte hotspotY
    ) {
        GlobalDataKey1 = globalDataKey1;
        GlobalDataKey2 = globalDataKey2;
        GdsNumber = gdsNumber;
        GdsLetter = gdsLetter;
        HasHotspot = hasHotspot;
        HotspotX = hotspotX;
        HotspotY = hotspotY;
    }

    /// <summary>
    /// The global whose value gates this encounter — a key for <c>GetGlobalValue</c> @0x42250.
    /// </summary>
    /// <remarks>
    /// <b>Unsigned, and it must be: the key space runs past 32767.</b> <c>GetGlobalValue</c>
    /// compares the key with <c>jb</c>/<c>jnb</c> — unsigned branches — and its top band is the
    /// 56000+ range backed by <c>global_flags2[]</c>. Reading the field as a signed 16-bit turns
    /// key 56012 into -9524, which no band then matches.
    ///
    /// <para>This is not hypothetical: the shipped OBJFIXED.DAT has one such record and the save
    /// games have more (56012 and 56315 both appear). Held as <c>int</c> rather than
    /// <c>ushort</c> so the value reads as the number the engine uses.</para>
    /// </remarks>
    public int GlobalDataKey1 { get; }

    /// <summary>
    /// The global this container WRITES when it is opened — <c>wGame_state_event_id</c>.
    /// </summary>
    /// <remarks>
    /// <b>*** NOT A GATE, AND THIS USED TO INHERIT Key1'S DOC SAYING IT WAS. ***</b>
    /// <c>cmbinv_inventory_screen_run</c> (canassa <c>SRC/SCREENS/CMBINV.C</c>) writes it as the
    /// container screen comes up, before the screen's loop:
    /// <code>
    /// pSub08 = actorrec_get_subrecord(actor, SUBREC_HOTSPOT);
    /// if (pSub08 != 0 &amp;&amp; pSub08->wGame_state_event_id != 0)
    ///     gstate_event_write(pSub08->wGame_state_event_id, 1);
    /// </code>
    /// In the 9-byte <c>ActorSubrec08_HotspotAction</c> (<c>wPad_0, wGame_state_event_id,
    /// bWarp_kind, bWarp_dest, bHas_hotspot, bHotspot_x, bHotspot_y</c>) this is the second word,
    /// which is why it is Key2 and not Key1.
    ///
    /// <para>The inherited wording cost a session: it reads as "something reads this", so the
    /// search for what sets Brother Jeremy's flag 56012 went to the hotspot post-key path instead
    /// of to the container screen. Six shipped containers carry one. TASK-560.</para>
    /// </remarks>
    public int GlobalDataKey2 { get; }
    public byte GdsNumber { get; }
    public byte GdsLetter { get; }

    /// <summary>
    /// 0x06. Nonzero when this location names a HOTSPOT at
    /// (<see cref="HotspotX"/>, <see cref="HotspotY"/>) for its click to dispatch.
    /// </summary>
    /// <remarks>
    /// <b>It was called <c>FiresTrapEncounter</c>, and the "trap" was wrong.</b> The original's
    /// subrecord is <c>hotspot_action { bHas_hotspot, bHotspot_x, bHotspot_y }</c> (WCURSOR.C:268)
    /// and the DISPATCH KIND is the caller's, not the field's:
    /// <c>wcursor_click_fixedobj_picklock</c> dispatches type <b>7</b> at WCURSOR.C:308 — a trap —
    /// while <c>wcursor_click_npc_or_trap</c> dispatches type <b>8</b>, a zone crossing, which is
    /// how the Mac Mordain Cadal's stairs are left. A name that says "trap" reads as
    /// "tunnels do not use this", which is exactly the wrong conclusion (TASK-400).
    ///
    /// <para>Every caller gates the same way: the party must be on the object's own map tile, then
    /// the hotspot at (X, Y) is dispatched before any GDS scene or dialog. Zero means a plain
    /// scene/dialog location.</para>
    /// </remarks>
    public byte HasHotspot { get; }

    /// <summary>The hotspot's grid X — the OBJECT's coordinate, not the party's.</summary>
    public byte HotspotX { get; }

    /// <inheritdoc cref="HotspotX"/>
    public byte HotspotY { get; }

    public bool HasHotspotSet {
        get => HasHotspot != 0;
    }

    /// <remarks><b>Deliberately callerless.</b> Serialized into the generated savegame JSON for readers of that output; no C# caller needs it.</remarks>
    public string? GdsFilename {
        get {
            if (GdsNumber == 0 || GdsLetter == 0) {
                return null;
            }

            char letter = (char)('A' + GdsLetter - 1);
            return $"GDS{GdsNumber}{letter}.DAT";
        }
    }
}
