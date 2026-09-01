namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The acting character's stats — <c>combat_actor_draw_stats_panel</c> (canassa CACTOR.C:1608).
/// </summary>
/// <remarks>
/// <b>This is the arena's DEFAULT panel, not a screen the player opens.</b> The turn loop draws it
/// whenever a command is armed and nothing more specific applies (COMBAT.C:2555) — which, since the
/// loop opens at <c>stateA = 1</c>, is most of a character's turn. Its absence is why the HUD strip
/// sat empty between actions.
///
/// <para><b>It is also what Inspect produces.</b> Inspecting an enemy has no panel of its own: the
/// arm clears the actor's Ready bit and switches the active actor, after which this draws for
/// whoever is now current. See <see cref="InspectAction"/>.</para>
///
/// <para><b>Party members only.</b> The routine's whole body is behind <c>actor-&gt;charSlot</c>,
/// so a monster's turn draws nothing at all — the parchment is not even blitted, which is a blank
/// strip rather than an empty panel.</para>
/// </remarks>
public static class ActorStatsPanel {
    /// <summary>The acting character's portrait, drawn beside the panel.</summary>
    /// <remarks>
    /// <b>The same routine draws the head and the text</b>, from
    /// <c>g_pHeadsBmxAssetTable[actor-&gt;charSlot - 1]</c> — so the face in the HUD is whoever is
    /// acting, not a fixed party slot. <b>COMBAT.DAT and SHOOT.DAT carry no portrait elements at
    /// all</b> (only the hidden character-screen zone, id 22, sits in this region), so the travel
    /// HUD's three portraits are not drawn during a fight and this is the only head on screen.
    ///
    /// <para>It is blitted to page 2 and nothing overdraws it — the parchment starts at
    /// <see cref="ShootTargetPanel.PanelX"/>, well to the right — so it stays up through the shoot,
    /// melee and assessment panels even though only this routine draws it.</para>
    /// </remarks>
    public const string PortraitImage = "HEADS.BMX";

    /// <inheritdoc cref="PortraitImage"/>
    public const int PortraitX = 0x0e;

    /// <inheritdoc cref="PortraitImage"/>
    public const int PortraitY = 0x8f;

    /// <summary>Left edge of the stat labels.</summary>
    public const int LabelX = 0x4e;

    /// <summary>Left edge of the stat values.</summary>
    /// <remarks>
    /// <b>0x85, not the shoot panel's 0x99.</b> The two panels put their values in different
    /// columns; sharing one constant would move this one 20 pixels right.
    /// </remarks>
    public const int ValueX = 0x85;

    /// <summary>Baseline of the character's name, centred on <see cref="ShootTargetPanel.CentreX"/>.</summary>
    public const int NameY = 0x84;

    /// <summary>Rows between the name and the first stat.</summary>
    public const int FirstRowOffset = 0xc;

    /// <summary>Rows between consecutive stats.</summary>
    public const int RowStep = 0xa;

    /// <summary>
    /// The four stats, in the order they are drawn, with the labels the original writes.
    /// </summary>
    /// <remarks>
    /// <b>Four, and these four.</b> They are attributes 0..3 — the pools and the two physical
    /// numbers — and none of the skills. The panel is a status readout, not a character sheet.
    /// </remarks>
    public static readonly (ActorAttribute Attribute, string Label)[] Rows = {
        (ActorAttribute.Health, "Health:"),
        (ActorAttribute.Stamina, "Stamina:"),
        (ActorAttribute.Speed, "Speed:"),
        (ActorAttribute.Strength, "Strength:"),
    };

    /// <summary>Top of the row at <paramref name="index"/>, counting from zero.</summary>
    public static int RowTop(int index) => NameY + FirstRowOffset + RowStep * index;

    /// <summary>
    /// What the panel puts on the parchment.
    /// </summary>
    /// <param name="name">The character's name.</param>
    /// <param name="values">One value per <see cref="Rows"/> entry, in the same order.</param>
    /// <returns>The lines, or an empty list when there is nothing to draw.</returns>
    public static IReadOnlyList<HudPanelLine> Lines(string name, IReadOnlyList<int> values) {
        var lines = new List<HudPanelLine>();
        if (name == null || values == null || values.Count < Rows.Length) {
            return lines;
        }
        lines.Add(new HudPanelLine(
            name, ShootTargetPanel.CentreX, NameY, HudPanelAlign.Centre));
        for (var row = 0; row < Rows.Length; row++) {
            int top = RowTop(row);
            lines.Add(new HudPanelLine(Rows[row].Label, LabelX, top));
            lines.Add(new HudPanelLine(values[row].ToString(), ValueX, top));
        }
        return lines;
    }
}
