namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// The parchment panel the SHOOT menu aims under —
/// <c>combat_arena_draw_tgt_info_panel</c> (canassa COMBAT.C:1152).
/// </summary>
/// <remarks>
/// <b>It is the shoot menu's own panel, not the arena's.</b> The turn loop raises it from
/// COMBAT.C:2543 only while <c>menu == g_shoot_menu</c>, and redraws it whenever the cursor moves
/// to another tile (<c>hudState = 2</c>, COMBAT.C:2536) — so it is a live readout, and the
/// accuracy on it is the accuracy of the shot you are about to take.
///
/// <para><b>It always says something.</b> With a valid target under the cursor it shows Accuracy
/// and Damage; with nothing under the cursor it shows how many quarrels of the chosen kind are
/// left. Reading the routine as "accuracy appears on hover" misses that second line, which is what
/// the panel shows for most of the time it is up.</para>
///
/// <para><b>The displayed accuracy is floored at 2; the shot's own chance is not.</b>
/// <see cref="CombatFormulas.RangedHitChance"/> floors at 0 and <see cref="CombatFormulas.RangedHits"/>
/// has no floor, so a shot the panel advertises as 2% can be a certain miss. That is the original's
/// behaviour — the floor is applied to the <i>string</i>, after the same call the roll uses.</para>
/// </remarks>
public static class ShootTargetPanel {
    /// <summary>The parchment the panel is drawn on, at <see cref="PanelX"/>,<see cref="PanelY"/>.</summary>
    public const string PanelImage = "PARCH.BMX";

    // Geometry in original 320x200 px, as immediate operands of the draw routine — the same shape
    // SpellEffectCaption states its plaque in, and for the same reason: nothing reads these from a
    // resource, so they are transcribed once here rather than spelled into the view.
    /// <summary>Left edge of the parchment.</summary>
    public const int PanelX = 0x49;

    /// <summary>Top edge of the parchment.</summary>
    public const int PanelY = 0x81;

    /// <summary>The x every centred line centres on.</summary>
    public const int CentreX = 0x82;

    /// <summary>Baseline of "Choose a target".</summary>
    public const int PromptY = 0x84;

    /// <summary>Distance between consecutive text lines.</summary>
    public const int LineStep = 10;

    /// <summary>Extra gap between the name and the stat block.</summary>
    public const int StatsGap = 12;

    /// <summary>Left edge of the stat labels.</summary>
    public const int LabelX = 0x4e;

    /// <summary>Left edge of the stat values.</summary>
    public const int ValueX = 0x99;

    /// <summary>Left edge of the quarrel-count line, which is indented five past the labels.</summary>
    public const int NoTargetX = LabelX + 5;

    /// <summary>The heading, centred on <see cref="CentreX"/>.</summary>
    public const string Prompt = "Choose a target";

    /// <summary>Label of the hit-chance row.</summary>
    public const string AccuracyLabel = "Accuracy:";

    /// <summary>Label of the damage row.</summary>
    public const string DamageLabel = "Damage:";

    /// <summary>Drawn one pixel past the accuracy value. The damage value has none.</summary>
    public const string PercentSign = "%";

    /// <summary>Follows the count on the no-target line: "<c>7 quarrels remaining</c>".</summary>
    public const string QuarrelsRemainingLabel = "quarrels remaining";

    /// <summary>Top of the name line at <paramref name="index"/>, counting from zero.</summary>
    public static int NameLineTop(int index) => PromptY + LineStep * (index + 1);

    /// <summary>
    /// Top of the first stat row — <b>which moves with the name</b>.
    /// </summary>
    /// <remarks>
    /// The routine walks one <c>y</c> down the panel, so a quarrel whose name wraps pushes the
    /// Accuracy row a line lower than one whose name does not. Pinning the stat rows at a fixed y
    /// would misplace them for every two-line name, which is most of them.
    /// </remarks>
    public static int StatsTop(int nameLineCount) => PromptY + LineStep * nameLineCount + StatsGap;

    /// <summary>Top of the Damage row, one line under <see cref="StatsTop"/>.</summary>
    public static int DamageTop(int nameLineCount) => StatsTop(nameLineCount) + LineStep;

    /// <summary>The accuracy as the panel prints it.</summary>
    /// <remarks>See the type remarks: the floor is on the display, not on the roll.</remarks>
    public static int DisplayedAccuracy(int rawHitChance) =>
        rawHitChance < CombatFormulas.MinHitChance ? CombatFormulas.MinHitChance : rawHitChance;

    /// <summary>
    /// Whether the thing under the cursor is a target the panel will report on.
    /// </summary>
    /// <remarks>
    /// <b>Three gates, and a party member fails the third.</b> The routine clears its target unless
    /// it is alive, its tile passes <c>combatgrid_tile_has_terr_bit2</c>, and
    /// <c>combatenc_is_encounter_actor</c> answers yes — the same three
    /// <see cref="InspectAction"/> uses. So hovering a companion shows the quarrel count, not an
    /// accuracy, and a port that only checks "alive" quotes a hit chance against your own party.
    /// </remarks>
    public static bool ShowsTargetStats(bool alive, bool onOpenTile, bool encounterActor) =>
        alive && onOpenTile && encounterActor;

    /// <summary>
    /// The quarrel kind whose NAME and COUNT the panel shows: the one under the cursor on the shoot
    /// menu, else the one already chosen.
    /// </summary>
    /// <param name="hoveredActionId">The shoot-menu entry under the cursor, or -1 for none.</param>
    /// <param name="selectedKind">The kind the player has clicked.</param>
    /// <param name="countOfKind">How many of a kind the shooter carries.</param>
    /// <remarks>
    /// <b>Only the name and the count follow the cursor — the accuracy and damage do not.</b> The
    /// routine reads the hovered kind for <c>quarrelRec</c> and for the remaining count, then passes
    /// <c>g_combat_menu_selected_item</c> to both <c>compute_hit_chance</c> and
    /// <c>calc_weapon_damage</c>. So hovering an unchosen quarrel names that quarrel while quoting
    /// the chosen one's numbers. It reads like an oversight and it is what ships.
    ///
    /// <para><b>A kind the shooter has none of does not preview</b>: the hover is dropped when the
    /// count is zero, falling back to the selection.</para>
    /// </remarks>
    public static int PreviewedKind(int hoveredActionId, int selectedKind, Func<int, int> countOfKind) {
        int hovered = CombatMenuSlots.QuarrelKindFor(hoveredActionId);
        if (hovered >= 0 && (countOfKind == null || countOfKind(hovered) == 0)) {
            hovered = -1;
        }
        return hovered >= 0 ? hovered : selectedKind;
    }

    /// <summary>What the panel puts on the parchment, positioned in original px.</summary>
    /// <remarks>
    /// Shared shape with <see cref="MeleeStatsPanel.Lines"/> so one view draws either — the panels
    /// differ in content, not in how they are rendered.
    /// </remarks>
    public static System.Collections.Generic.IReadOnlyList<HudPanelLine> Lines(
        ShootTargetPanelContent content) {
        var lines = new System.Collections.Generic.List<HudPanelLine> {
            new HudPanelLine(Prompt, CentreX, PromptY, HudPanelAlign.Centre),
        };
        if (content == null) {
            return lines;
        }
        for (var line = 0; line < content.NameLines.Count; line++) {
            lines.Add(new HudPanelLine(
                content.NameLines[line], CentreX, NameLineTop(line), HudPanelAlign.Centre));
        }

        int statsTop = StatsTop(content.NameLines.Count);
        if (!content.HasTarget) {
            lines.Add(new HudPanelLine(
                content.QuarrelsRemaining + " " + QuarrelsRemainingLabel, NoTargetX, statsTop));
            return lines;
        }

        lines.Add(new HudPanelLine(AccuracyLabel, LabelX, statsTop));
        lines.Add(new HudPanelLine(content.Accuracy + PercentSign, ValueX, statsTop));
        int damageTop = DamageTop(content.NameLines.Count);
        lines.Add(new HudPanelLine(DamageLabel, LabelX, damageTop));
        lines.Add(new HudPanelLine(content.Damage.ToString(), ValueX, damageTop));
        return lines;
    }

    /// <summary>The panel with a target under the cursor: accuracy and damage.</summary>
    public static ShootTargetPanelContent ForTarget(
        IReadOnlyList<string> nameLines, int rawHitChance, int damage) =>
        new ShootTargetPanelContent(nameLines, true, DisplayedAccuracy(rawHitChance), damage, 0);

    /// <summary>The panel with nothing valid under the cursor: how much ammunition is left.</summary>
    public static ShootTargetPanelContent WithoutTarget(
        IReadOnlyList<string> nameLines, int quarrelsRemaining) =>
        new ShootTargetPanelContent(nameLines, false, 0, 0, quarrelsRemaining);
}

/// <summary>What <see cref="ShootTargetPanel"/> puts on the parchment for one cursor position.</summary>
public sealed class ShootTargetPanelContent {
    internal ShootTargetPanelContent(IReadOnlyList<string> nameLines, bool hasTarget,
        int accuracy, int damage, int quarrelsRemaining) {
        NameLines = nameLines ?? Array.Empty<string>();
        HasTarget = hasTarget;
        Accuracy = accuracy;
        Damage = damage;
        QuarrelsRemaining = quarrelsRemaining;
    }

    /// <summary>The chosen quarrel's name, one or two lines.</summary>
    public IReadOnlyList<string> NameLines { get; }

    /// <summary>True when the stat rows are shown instead of the quarrel count.</summary>
    public bool HasTarget { get; }

    /// <summary>Hit chance as printed — see <see cref="ShootTargetPanel.DisplayedAccuracy"/>.</summary>
    public int Accuracy { get; }

    /// <summary>Damage the shot would do.</summary>
    public int Damage { get; }

    /// <summary>Quarrels of the previewed kind still in the pack.</summary>
    public int QuarrelsRemaining { get; }
}
