namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The SHOOT menu's target panel — <c>combat_arena_draw_tgt_info_panel</c> (COMBAT.C:1152).
/// </summary>
public class ShootTargetPanelTests {
    [Fact]
    public void TheAccuracyFloorIsOnTheDISPLAY_NotOnTheShot() {
        // The panel never prints below 2, but the same call the roll uses floors at 0 and
        // RangedHits has no floor at all -- so a panel reading "2%" can be a certain miss. A port
        // that moved this floor into the formula would make impossible shots land 2% of the time.
        Assert.Equal(2, ShootTargetPanel.DisplayedAccuracy(0));
        Assert.Equal(2, ShootTargetPanel.DisplayedAccuracy(1));
        Assert.Equal(37, ShootTargetPanel.DisplayedAccuracy(37));
        Assert.Equal(0, CombatFormulas.RangedHitChance(baseSkill: 4, chebyshevDistance: 40, ammoAccuracyBonus: 0));
        Assert.False(CombatFormulas.RangedHits(roll: 1, hitChance: 0));
    }

    [Fact]
    public void APartyMemberUnderTheCursorShowsNoAccuracy() {
        // Three gates, and a companion fails the encounter-actor one. Checking only "alive" would
        // quote a hit chance against your own party.
        Assert.True(ShootTargetPanel.ShowsTargetStats(alive: true, onOpenTile: true, encounterActor: true));
        Assert.False(ShootTargetPanel.ShowsTargetStats(alive: true, onOpenTile: true, encounterActor: false));
        Assert.False(ShootTargetPanel.ShowsTargetStats(alive: false, onOpenTile: true, encounterActor: true));
        Assert.False(ShootTargetPanel.ShowsTargetStats(alive: true, onOpenTile: false, encounterActor: true));
    }

    [Fact]
    public void TheStatRowsMoveDownWhenTheQuarrelNameWraps() {
        // The routine walks one y down the panel, so the Accuracy row sits a line lower for a
        // two-line name. Fixed stat-row coordinates would misplace it for most quarrels.
        Assert.Equal(ShootTargetPanel.StatsTop(1) + ShootTargetPanel.LineStep,
            ShootTargetPanel.StatsTop(2));
        Assert.Equal(ShootTargetPanel.StatsTop(2) + ShootTargetPanel.LineStep,
            ShootTargetPanel.DamageTop(2));
        // First name line always sits one step under the prompt, whichever count it is.
        Assert.Equal(ShootTargetPanel.PromptY + ShootTargetPanel.LineStep,
            ShootTargetPanel.NameLineTop(0));
        // The gap is measured from the LAST name line, not from a line the name did not have -- an
        // off-by-one here shifts both stat rows for every quarrel and the relative checks above
        // stay green through it.
        Assert.Equal(ShootTargetPanel.NameLineTop(0) + ShootTargetPanel.StatsGap,
            ShootTargetPanel.StatsTop(1));
        Assert.Equal(ShootTargetPanel.NameLineTop(1) + ShootTargetPanel.StatsGap,
            ShootTargetPanel.StatsTop(2));
    }

    [Fact]
    public void HoveringAQuarrelButtonRenamesThePanelButDoesNotRestateTheNumbers() {
        // Only the name and the count follow the cursor: compute_hit_chance and calc_weapon_damage
        // are both passed g_combat_menu_selected_item. PreviewedKind is therefore asked for the
        // name/count alone, and the caller keeps using the selection for the stats.
        int hoveredKind3Id = CombatMenuSlots.ActionIdByQuarrelKind[3];
        Assert.Equal(3, ShootTargetPanel.PreviewedKind(hoveredKind3Id, selectedKind: 0, _ => 5));
        // Carrying none of the hovered kind falls back to the selection.
        Assert.Equal(0, ShootTargetPanel.PreviewedKind(hoveredKind3Id, selectedKind: 0, _ => 0));
        // Nothing hovered, and a non-quarrel button (Back), both keep the selection.
        Assert.Equal(6, ShootTargetPanel.PreviewedKind(-1, selectedKind: 6, _ => 5));
        Assert.Equal(6, ShootTargetPanel.PreviewedKind(
            CombatMenuSlots.DistinctEnableGateActionId, selectedKind: 6, _ => 5));
    }

    [Fact]
    public void WithNoTargetThePanelStillSaysHowMuchAmmunitionIsLeft() {
        // "accuracy appears on hover" misses this line, which is what the panel shows for most of
        // the time it is up.
        ShootTargetPanelContent idle =
            ShootTargetPanel.WithoutTarget(new[] { "Crossbow", "Bolts" }, quarrelsRemaining: 12);
        Assert.False(idle.HasTarget);
        Assert.Equal(12, idle.QuarrelsRemaining);

        ShootTargetPanelContent aiming =
            ShootTargetPanel.ForTarget(new[] { "Crossbow", "Bolts" }, rawHitChance: 1, damage: 9);
        Assert.True(aiming.HasTarget);
        Assert.Equal(2, aiming.Accuracy);
        Assert.Equal(9, aiming.Damage);
    }

    [Fact]
    public void TheNameSplitDropsTheByteAtTheOffset() {
        // Shared with the inventory inspect panel: the original NULs the byte at wordWrap and
        // starts the second line one past it, so the space is dropped rather than kept.
        IReadOnlyList<string> lines = ObjectNameLines.Split("Tsurani Bolts", 7);
        Assert.Equal(new[] { "Tsurani", "Bolts" }, lines);
        Assert.Equal(new[] { "Rock" }, ObjectNameLines.Split("Rock", 0));
        // An offset past the end is a single line, not a crash.
        Assert.Equal(new[] { "Rock" }, ObjectNameLines.Split("Rock", 9));
    }
}
