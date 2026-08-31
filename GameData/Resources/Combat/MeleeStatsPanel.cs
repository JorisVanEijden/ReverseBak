namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The melee attack preview — <c>combat_arena_hud_melee_panel</c> (canassa COMBAT.C:1240).
/// </summary>
/// <remarks>
/// <b>It is not the idle HUD and it is not the melee menu's panel.</b> The turn loop draws it under
/// <c>stateA == 0</c> alone (COMBAT.C:2566), and the loop <i>opens</i> at <c>stateA = 1</c> — 0 is
/// written in exactly one place, the arm where the cursor is over a reachable enemy and a click
/// would attack it. So this is the melee counterpart of
/// <see cref="ShootTargetPanel"/>: the numbers for the attack the next click would make, against
/// the enemy under the cursor. (The idle panel in state 1 is the actor stats panel, a different
/// one.)
///
/// <para><b>Its bottom row is the only place the game tells the player about the two buttons.</b>
/// "Left" sits under the thrust column and "Right" under the swing column, with no values — they
/// are labels for the mouse buttons, not for anything above them.</para>
///
/// <para><b>THE NUMBERS ARE AN ESTIMATE, NOT THE ROLL.</b> Accuracy here is a bare
/// <c>meleeAccuracy + weaponAccuracy</c>: no class affinity, no condition scaling, no blessing, and
/// <b>no subtraction of the target's defence</b>, all of which <see cref="CombatFormulas.MeleeHitChance"/>
/// applies. Damage likewise skips the weapon's condition scaling that
/// <see cref="CombatFormulas.MeleeDamage"/> applies. The panel therefore reads high against a
/// defended target and high again for a worn weapon — reproduce it as shipped rather than
/// "correcting" it to the real chance, which would make the readout disagree with the original for
/// every character in the game.</para>
/// </remarks>
public static class MeleeStatsPanel {
    /// <summary>Left edge of the thrust column, and of the two row labels under it.</summary>
    public const int ThrustX = 0x53;

    /// <summary>Right edge the swing column's headings grow leftward from — <c>0x73 + 0x3f</c>.</summary>
    public const int SwingRightX = 0xb2;

    /// <summary>Top of the column headings.</summary>
    public const int HeadingY = 0x86;

    /// <summary>Width of the bevelled rule under the thrust heading.</summary>
    public const int RuleWidth = 0x5e;

    /// <summary>Rows between the heading and the damage row.</summary>
    public const int DamageRowOffset = 0xf;

    /// <summary>Rows between the damage row and the accuracy row.</summary>
    public const int AccuracyRowOffset = 0xa;

    /// <summary>Rows between the accuracy row and the button row.</summary>
    public const int ButtonRowOffset = 0xc;

    /// <summary>x the row labels ("Damage", "Accuracy") centre on.</summary>
    public const int RowLabelCentreX = ShootTargetPanel.CentreX;

    /// <summary>
    /// x the swing DAMAGE value centres on.
    /// </summary>
    /// <remarks>
    /// <b>The two swing values do not share a centre</b> — damage centres on 0xad and accuracy on
    /// 0xa5, eight pixels apart, because the accuracy arm subtracts a further 0x17 from a 0xbc
    /// origin. Reproduced rather than tidied: the columns really are not aligned with each other.
    /// </remarks>
    public const int SwingDamageCentreX = 0xad;

    /// <inheritdoc cref="SwingDamageCentreX"/>
    public const int SwingAccuracyCentreX = 0xbc - 0x17;

    /// <summary>Column headings.</summary>
    public const string ThrustHeading = "Thrust";

    /// <inheritdoc cref="ThrustHeading"/>
    public const string SwingHeading = "Swing";

    /// <summary>Row labels.</summary>
    public const string DamageLabel = "Damage";

    /// <inheritdoc cref="DamageLabel"/>
    public const string AccuracyLabel = "Accuracy";

    /// <summary>The mouse-button labels on the bottom row.</summary>
    public const string ThrustButton = "Left";

    /// <inheritdoc cref="ThrustButton"/>
    public const string SwingButton = "Right";

    /// <summary>Palette pens of the two rows of the bevelled rule, in draw order.</summary>
    public static readonly int[] RulePens = { 2, 3 };

    /// <summary>Damage never reads below this.</summary>
    public const int MinDamage = 1;

    /// <summary>
    /// <b>The whole swing column is hidden unless a swing is possible.</b>
    /// </summary>
    /// <remarks>
    /// The same two conditions the right-click arm tests — the target orthogonally adjacent, and the
    /// attacker's combined health+stamina above 1 — so the column is absent exactly when the button
    /// would refuse. The heading, both values and the "Right" label all go; the thrust column stays.
    ///
    /// <para>The gates read <c>stat_actor_get(actor, 0x10, <b>0</b>)</c> here against
    /// <c>(actor, 0x10, <b>4</b>)</c> in the click arm. <b>They cannot disagree</b>: mode 4 only
    /// skips the health-ratio scaling, and that ratio is 0 for Health and Stamina
    /// (<c>StatEngine</c>'s <c>HealthRatio</c> table), which is what stat 0x10 sums.</para>
    /// </remarks>
    public static bool ShowsSwingColumn(bool targetOrthogonallyAdjacent, int healthStaminaPool) =>
        targetOrthogonallyAdjacent
        && CombatActionDispatch.HasReservesFor(
            CombatActionDispatch.MeleeAttack.Swing, healthStaminaPool);

    /// <summary>
    /// The accuracy the panel prints — <b>not the hit chance</b>. See the type remarks.
    /// </summary>
    public static int AccuracyShown(int meleeAccuracy, int weaponAccuracy) {
        int shown = meleeAccuracy + weaponAccuracy;
        return shown < CombatFormulas.MinHitChance ? CombatFormulas.MinHitChance : shown;
    }

    /// <summary>
    /// The damage the panel prints — <b>not the damage roll</b>. See the type remarks.
    /// </summary>
    public static int DamageShown(int weaponBase, int strength, int enchantmentBonus) {
        int shown = weaponBase + strength + enchantmentBonus;
        return shown < MinDamage ? MinDamage : shown;
    }

    /// <summary>What the panel puts on the parchment.</summary>
    /// <param name="showSwing">From <see cref="ShowsSwingColumn"/>.</param>
    public static IReadOnlyList<HudPanelLine> Lines(
        bool showSwing, int thrustDamage, int thrustAccuracy, int swingDamage, int swingAccuracy) {
        int damageY = HeadingY + DamageRowOffset;
        int accuracyY = damageY + AccuracyRowOffset;
        int buttonY = accuracyY + ButtonRowOffset;

        var lines = new List<HudPanelLine> {
            new HudPanelLine(ThrustHeading, ThrustX, HeadingY),
            new HudPanelLine(DamageLabel, RowLabelCentreX, damageY, HudPanelAlign.Centre),
            new HudPanelLine(thrustDamage.ToString(), ThrustX, damageY),
            new HudPanelLine(AccuracyLabel, RowLabelCentreX, accuracyY, HudPanelAlign.Centre),
            new HudPanelLine(
                thrustAccuracy + ShootTargetPanel.PercentSign, ThrustX, accuracyY),
            new HudPanelLine(ThrustButton, ThrustX, buttonY),
        };
        if (!showSwing) {
            return lines;
        }

        lines.Add(new HudPanelLine(SwingHeading, SwingRightX, HeadingY, HudPanelAlign.Right));
        lines.Add(new HudPanelLine(
            swingDamage.ToString(), SwingDamageCentreX, damageY, HudPanelAlign.Centre));
        lines.Add(new HudPanelLine(swingAccuracy + ShootTargetPanel.PercentSign,
            SwingAccuracyCentreX, accuracyY, HudPanelAlign.Centre));
        lines.Add(new HudPanelLine(SwingButton, SwingRightX, buttonY, HudPanelAlign.Right));
        return lines;
    }

    /// <summary>The bevelled underline below the thrust heading. Always drawn, both rows.</summary>
    public static IReadOnlyList<HudPanelRule> Rules() {
        var rules = new List<HudPanelRule>();
        for (var row = 0; row < RulePens.Length; row++) {
            rules.Add(new HudPanelRule(
                ThrustX, HeadingY + 0xa + row, RuleWidth, RulePens[row]));
        }
        return rules;
    }
}
