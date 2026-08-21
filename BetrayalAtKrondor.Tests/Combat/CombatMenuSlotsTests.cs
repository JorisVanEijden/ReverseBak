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
    public void TheTwoShootPagesCoverTheSameFourCells() {
        Assert.Equal(4, CombatMenuSlots.FirstPageActionIds.Length);
        Assert.Equal(4, CombatMenuSlots.SecondPageActionIds.Length);

        var seen = new HashSet<int>();
        foreach (int id in CombatMenuSlots.FirstPageActionIds) {
            Assert.True(seen.Add(id));
        }
        foreach (int id in CombatMenuSlots.SecondPageActionIds) {
            Assert.True(seen.Add(id), "a quarrel id belongs to exactly one page");
        }
    }

    [Fact]
    public void PageFlippingIsAToggleBetweenExactlyTwo() {
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.FlipPage(CombatMenuSlots.FirstPage));
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.FlipPage(CombatMenuSlots.SecondPage));
        Assert.Equal(50, CombatMenuSlots.PageFlipActionId);
    }

    [Fact]
    public void AQuarrelButtonNeedsBOTHItsPageShowingAndAmmunition() {
        // *** The condition a port drops. *** Page alone is not enough: the menu shows only the
        // ammunition you actually carry, so an empty kind greys out instead of being clickable and
        // then failing.
        Assert.True(CombatMenuSlots.QuarrelIsAvailable(2, CombatMenuSlots.FirstPage, quarrelsOfThatKind: 5));
        Assert.False(CombatMenuSlots.QuarrelIsAvailable(2, CombatMenuSlots.FirstPage, quarrelsOfThatKind: 0),
            "no quarrels of that kind");
        Assert.False(CombatMenuSlots.QuarrelIsAvailable(2, CombatMenuSlots.SecondPage, quarrelsOfThatKind: 5),
            "its page is not showing");
    }

    [Fact]
    public void EachQuarrelIdKnowsItsPage_AndNonQuarrelIdsHaveNone() {
        Assert.Equal(CombatMenuSlots.FirstPage, CombatMenuSlots.PageOf(3));
        Assert.Equal(CombatMenuSlots.SecondPage, CombatMenuSlots.PageOf(8));
        Assert.Equal(0, CombatMenuSlots.PageOf(CombatMenuSlots.PageFlipActionId));
        Assert.Equal(0, CombatMenuSlots.PageOf(CombatMenuSlots.CastActionId));
    }

    [Fact]
    public void ThePageFlipItselfIsNeverAQuarrelButton() {
        // It shares the HUD with them but is not one of the four cells, so it must not be gated on
        // ammunition — a player with no quarrels at all still needs to be able to flip back.
        Assert.False(CombatMenuSlots.QuarrelIsAvailable(
            CombatMenuSlots.PageFlipActionId, CombatMenuSlots.FirstPage, quarrelsOfThatKind: 0));
        Assert.Equal(0, CombatMenuSlots.PageOf(CombatMenuSlots.PageFlipActionId));
    }
}
