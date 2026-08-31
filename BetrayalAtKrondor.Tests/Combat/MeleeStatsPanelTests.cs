namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Linq;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The melee attack preview — <c>combat_arena_hud_melee_panel</c> (COMBAT.C:1240).
/// </summary>
public class MeleeStatsPanelTests {
    private static IReadOnlyList<HudPanelLine> Panel(bool showSwing) =>
        MeleeStatsPanel.Lines(showSwing, thrustDamage: 7, thrustAccuracy: 41,
            swingDamage: 12, swingAccuracy: 33);

    private static bool Has(IReadOnlyList<HudPanelLine> lines, string text) =>
        lines.Any(l => l.Text == text);

    [Fact]
    public void TheWholeSwingCOLUMNGoesWhenASwingIsImpossible() {
        // Not just the number: the heading, both values and the "Right" label all go, because the
        // original guards each of them with the same hideSwing flag. The thrust column stays.
        IReadOnlyList<HudPanelLine> hidden = Panel(showSwing: false);
        Assert.False(Has(hidden, MeleeStatsPanel.SwingHeading));
        Assert.False(Has(hidden, MeleeStatsPanel.SwingButton));
        Assert.False(Has(hidden, "12"));
        Assert.False(Has(hidden, "33%"));
        Assert.True(Has(hidden, MeleeStatsPanel.ThrustHeading));
        Assert.True(Has(hidden, MeleeStatsPanel.ThrustButton));
        Assert.True(Has(hidden, "7"));
        Assert.True(Has(hidden, "41%"));

        Assert.True(Has(Panel(showSwing: true), MeleeStatsPanel.SwingHeading));
    }

    [Fact]
    public void TheColumnIsHiddenExactlyWhenTheRightButtonWouldRefuse() {
        // Same two conditions the click arm tests, so the panel is a promise about the button.
        Assert.True(MeleeStatsPanel.ShowsSwingColumn(
            targetOrthogonallyAdjacent: true, healthStaminaPool: 2));
        Assert.False(MeleeStatsPanel.ShowsSwingColumn(
            targetOrthogonallyAdjacent: false, healthStaminaPool: 40));
        Assert.False(MeleeStatsPanel.ShowsSwingColumn(
            targetOrthogonallyAdjacent: true, healthStaminaPool: 1));
    }

    [Fact]
    public void TheBottomRowNamesTheMOUSEBUTTONS_NotTheColumnsAgain() {
        // "Left" and "Right" carry no values and are the only place the game says which button
        // makes which attack. Left sits in the thrust column, Right in the swing one.
        IReadOnlyList<HudPanelLine> lines = Panel(showSwing: true);
        HudPanelLine left = lines.Single(l => l.Text == MeleeStatsPanel.ThrustButton);
        HudPanelLine right = lines.Single(l => l.Text == MeleeStatsPanel.SwingButton);
        HudPanelLine thrustHeading = lines.Single(l => l.Text == MeleeStatsPanel.ThrustHeading);
        HudPanelLine swingHeading = lines.Single(l => l.Text == MeleeStatsPanel.SwingHeading);

        Assert.Equal(thrustHeading.X, left.X);
        Assert.Equal(swingHeading.X, right.X);
        Assert.Equal(HudPanelAlign.Right, right.Align);
        Assert.True(left.Y > thrustHeading.Y, "the button row is the last one");
    }

    [Fact]
    public void TheTwoSwingVALUESDoNotShareACentre() {
        // Damage centres on 0xad and accuracy on 0xa5 -- the accuracy arm subtracts a further 0x17
        // from a 0xbc origin. The columns really are not aligned with each other; tidying that up
        // would move a number the original does not.
        IReadOnlyList<HudPanelLine> lines = Panel(showSwing: true);
        HudPanelLine damage = lines.Single(l => l.Text == "12");
        HudPanelLine accuracy = lines.Single(l => l.Text == "33%");
        Assert.NotEqual(damage.X, accuracy.X);
        Assert.Equal(0xad, damage.X);
        Assert.Equal(0xa5, accuracy.X);
    }

    [Fact]
    public void TheNumbersAreAnESTIMATE_NotTheRoll() {
        // Accuracy is a bare skill+weapon sum: no class affinity, no condition scaling, and no
        // subtraction of the target's defence, all of which MeleeHitChance applies. A port that
        // "corrected" this to the real chance would disagree with the original for every character.
        Assert.Equal(70, MeleeStatsPanel.AccuracyShown(meleeAccuracy: 40, weaponAccuracy: 30));
        Assert.NotEqual(
            MeleeStatsPanel.AccuracyShown(meleeAccuracy: 40, weaponAccuracy: 30),
            CombatFormulas.MeleeHitChance(accuracyMelee: 40, hasWeapon: true, weaponAccuracy: 30,
                classGroupModifier: 0, weaponConditionPercent: 100, weaponFlags: 0,
                targetDefenseRating: 20));

        // Both readouts have floors, and they are the panel's own.
        Assert.Equal(CombatFormulas.MinHitChance,
            MeleeStatsPanel.AccuracyShown(meleeAccuracy: 0, weaponAccuracy: 0));
        Assert.Equal(MeleeStatsPanel.MinDamage,
            MeleeStatsPanel.DamageShown(weaponBase: 0, strength: 0, enchantmentBonus: 0));
        Assert.Equal(1, MeleeStatsPanel.MinDamage);
    }

    [Fact]
    public void TheRuleUnderTheThrustHeadingIsTwoRowsInDifferentPens() {
        // A bevel, not a line: pens 2 and 3 one row apart. Drawing one row loses the bevel.
        IReadOnlyList<HudPanelRule> rules = MeleeStatsPanel.Rules();
        Assert.Equal(2, rules.Count);
        Assert.Equal(rules[0].Y + 1, rules[1].Y);
        Assert.NotEqual(rules[0].Pen, rules[1].Pen);
        Assert.All(rules, r => Assert.Equal(MeleeStatsPanel.ThrustX, r.X));
    }
}
