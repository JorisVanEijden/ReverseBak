namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which button occupies each combat-HUD cell. Both menus ship alternates at one position, so
/// "which one" is the whole question.
/// </summary>
public class CombatMenuSlotsTests {
    [Fact]
    public void TheCapabilityCellShowsShootCastOrNeither() {
        Assert.Equal(CombatMenuSlots.ShootActionId, CombatMenuSlots.CapabilitySlot(true, false));
        Assert.Equal(CombatMenuSlots.CastActionId, CombatMenuSlots.CapabilitySlot(false, true));
        Assert.Equal(CombatMenuSlots.NeitherActionId, CombatMenuSlots.CapabilitySlot(false, false));
    }

    [Fact]
    public void ACharacterWhoCanDoBothShowsSHOOT() {
        // The original tests shooting first. Nothing in the data says so — all three entries sit at
        // the same cell and look alike — so this ordering only exists in the refresh routine.
        Assert.Equal(CombatMenuSlots.ShootActionId, CombatMenuSlots.CapabilitySlot(true, true));
    }

    [Fact]
    public void TheNeitherCaseIsALabel_NotAButton() {
        // It is drawn but always gated, which is why it is the one COMBAT entry shipped Disabled.
        Assert.False(CombatMenuSlots.CapabilitySlotIsClickable(CombatMenuSlots.NeitherActionId));
        Assert.True(CombatMenuSlots.CapabilitySlotIsClickable(CombatMenuSlots.ShootActionId));
        Assert.True(CombatMenuSlots.CapabilitySlotIsClickable(CombatMenuSlots.CastActionId));
    }

    [Fact]
    public void TheThreeCapabilityIdsAreTheOnesShippedAtOneCell() {
        // Guards the mapping against a re-extraction: 31/46/14 are the ids COMBAT.json puts at
        // (1000, 786), which is what makes them alternates in the first place.
        Assert.Equal(31, CombatMenuSlots.ShootActionId);
        Assert.Equal(46, CombatMenuSlots.CastActionId);
        Assert.Equal(14, CombatMenuSlots.NeitherActionId);
    }

    [Fact]
    public void EveryQuarrelKindHasAnIdAndTheMappingRoundTrips() {
        // The one static thing about this menu. Kind order is the original's item-id table.
        Assert.Equal(8, CombatMenuSlots.ActionIdByQuarrelKind.Length);
        for (var kind = 0; kind < 8; kind++) {
            Assert.Equal(kind, CombatMenuSlots.QuarrelKindFor(CombatMenuSlots.ActionIdByQuarrelKind[kind]));
        }
        Assert.Equal(-1, CombatMenuSlots.QuarrelKindFor(CombatMenuSlots.PageFlipActionId));
        Assert.Equal(-1, CombatMenuSlots.QuarrelKindFor(CombatMenuSlots.CastActionId));
    }

    [Fact]
    public void PageFlippingIsAToggleBetweenExactlyTwo() {
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.FlipPage(CombatMenuSlots.FirstPage));
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.FlipPage(CombatMenuSlots.SecondPage));
        Assert.Equal(50, CombatMenuSlots.PageFlipActionId);
    }

    [Fact]
    public void APageIsTheFirstFourCellsAndTheNextFour() {
        // find_item_page is (index >> 2) + 1 — nothing about the id enters into it.
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.PageOfSlot(0));
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.PageOfSlot(3));
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.PageOfSlot(4));
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.PageOfSlot(7));
    }

    [Fact]
    public void CARRYINGFewerKindsPullsLaterOnesONTOTheFirstPage() {
        // *** The bug the old model had. *** It split the kind table down the middle and called the
        // halves the two pages, so kind 6 was "page two" for everyone. The menu is REPACKED per
        // actor: an archer carrying only kinds 5 and 6 has them in the first two cells, on page one.
        var carriesTwoLateKinds = new[] { 0, 0, 0, 0, 0, 3, 3, 0 };

        int[] cells = CombatMenuSlots.PackCells(carriesTwoLateKinds);

        Assert.Equal(CombatMenuSlots.ActionIdByQuarrelKind[5], cells[0]);
        Assert.Equal(CombatMenuSlots.ActionIdByQuarrelKind[6], cells[1]);
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.PageOfSlot(0));
        Assert.Equal(-1, cells[2]);
    }

    [Fact]
    public void AFullQuiverPacksInKindOrderAndFillsBothPages() {
        var carriesEverything = new[] { 1, 1, 1, 1, 1, 1, 1, 1 };

        int[] cells = CombatMenuSlots.PackCells(carriesEverything);

        Assert.Equal(CombatMenuSlots.ActionIdByQuarrelKind, cells);
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.PageOfSlot(4));
    }

    [Fact]
    public void AnEmptyQuiverClaimsNoCellAtAll() {
        int[] cells = CombatMenuSlots.PackCells(new[] { 0, 0, 0, 0, 0, 0, 0, 0 });

        Assert.All(cells, c => Assert.Equal(-1, c));
        // Null is the same as empty rather than a throw — an actor with no quiver at all is an
        // ordinary case, not a caller error.
        Assert.All(CombatMenuSlots.PackCells(null), c => Assert.Equal(-1, c));
        Assert.Equal(8, CombatMenuSlots.PackCells(null).Length);
    }

    [Fact]
    public void AQuarrelCellNeedsBOTHItsPageShowingAndAmmunition() {
        // The condition a port drops: the menu shows only ammunition actually carried, so an empty
        // kind greys out instead of being clickable and then failing.
        Assert.True(CombatMenuSlots.QuarrelIsAvailable(0, CombatMenuSlots.FirstPage, quarrelsOfThatKind: 5));
        Assert.False(CombatMenuSlots.QuarrelIsAvailable(0, CombatMenuSlots.FirstPage, quarrelsOfThatKind: 0),
            "no quarrels of that kind");
        Assert.False(CombatMenuSlots.QuarrelIsAvailable(0, CombatMenuSlots.SecondPage, quarrelsOfThatKind: 5),
            "its page is not showing");
    }
}
