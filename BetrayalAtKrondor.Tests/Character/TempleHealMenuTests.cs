namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The temple healing screen's loop (<c>charscreen_temple_heal_menu</c> @0x5877e, from 0x58846).
/// </summary>
public class TempleHealMenuTests {
    private const int WoundMode = TempleHealEntry.ModeThatTreatsWounds;
    private const int PlainMode = 1;

    // ---- the bill -----------------------------------------------------------------------------

    [Fact]
    public void ThePlainServiceBillsOnlyForAfflictions() =>
        Assert.Equal(40, TempleHealMenu.BillFor(afflictionPrice: 40, healthStaminaDeficit: 17, PlainMode));

    [Fact]
    public void TheWoundServiceAddsARoyalPerMissingPoint() =>
        // Not a discount or a surcharge — an extra line on the bill, which is also why that mode
        // will see a merely-wounded party at all.
        Assert.Equal(57, TempleHealMenu.BillFor(afflictionPrice: 40, healthStaminaDeficit: 17, WoundMode));

    [Fact]
    public void AnUnwoundedCharacterCostsTheSameUnderBothModes() =>
        Assert.Equal(
            TempleHealMenu.BillFor(40, healthStaminaDeficit: 0, PlainMode),
            TempleHealMenu.BillFor(40, healthStaminaDeficit: 0, WoundMode));

    // ---- what a cure does ----------------------------------------------------------------------

    [Fact]
    public void EverySicknessIsCleared() {
        for (var condition = 0; condition < TempleHealMenu.ConditionCount; condition++) {
            if (condition == TempleHealMenu.HealingCondition) {
                continue;
            }

            Assert.Equal(TempleHealMenu.ClearAmount, TempleHealMenu.CureAmountFor(condition, PlainMode));
        }
    }

    [Fact]
    public void HealingIsGrantedRatherThanCleared() {
        // The whole point of the visit. A port that "cures" by clearing all seven strips the one
        // condition the priest is there to give, and the character leaves with no regeneration.
        Assert.Equal(TempleHealMenu.HealingGrantedByCure,
            TempleHealMenu.CureAmountFor(TempleHealMenu.HealingCondition, PlainMode));
        Assert.True(TempleHealMenu.CureAmountFor(TempleHealMenu.HealingCondition, PlainMode) > 0);
    }

    [Fact]
    public void MendingWoundsGrantsFarMoreHealing() {
        Assert.Equal(TempleHealMenu.HealingGrantedByFullCure,
            TempleHealMenu.CureAmountFor(TempleHealMenu.HealingCondition, WoundMode));
        Assert.True(TempleHealMenu.HealingGrantedByFullCure > TempleHealMenu.HealingGrantedByCure);
    }

    [Fact]
    public void OnlyTheWoundServiceRestoresHealth() {
        Assert.True(TempleHealMenu.RestoresHealth(WoundMode));
        Assert.False(TempleHealMenu.RestoresHealth(PlainMode));
    }

    // ---- moving on ------------------------------------------------------------------------------

    private static System.Func<int, bool> Needy(params int[] slots) {
        var set = new HashSet<int>(slots);
        return set.Contains;
    }

    [Fact]
    public void NextSkipsAnyoneWhoNeedsNothing() =>
        // "The next member with something to cure", not "the next member".
        Assert.Equal(2, TempleHealMenu.NextNeedy(current: 0, partyCount: 3, Needy(2)));

    [Fact]
    public void NextDoesNotWrapAround() {
        // Running off the end closes the screen instead of returning to the top.
        int slot = TempleHealMenu.NextNeedy(current: 2, partyCount: 3, Needy(0));

        Assert.True(TempleHealMenu.ClosesAfter(slot, 3));
    }

    [Fact]
    public void NextAlwaysMovesAtLeastOnce() =>
        // A do-while, so standing on a needy member still advances — otherwise curing someone would
        // land back on them and the screen would never finish.
        Assert.NotEqual(1, TempleHealMenu.NextNeedy(current: 1, partyCount: 3, Needy(1, 2)));

    [Fact]
    public void CuringAdvancesByItself() =>
        // The cure arm ends by falling into Next, so healing a party is one pass and the screen
        // closes on its own after the last of them.
        Assert.True(TempleHealMenu.CureAdvances);

    [Fact]
    public void APartyWhereNobodyElseNeedsAnythingClosesImmediately() {
        int slot = TempleHealMenu.NextNeedy(current: 0, partyCount: 3, Needy(0));

        Assert.True(TempleHealMenu.ClosesAfter(slot, 3));
    }

    // ---- picking a character ---------------------------------------------------------------------

    [Fact]
    public void ThePortraitRowSelectsDirectly() {
        Assert.Equal(0, TempleHealMenu.PartySlotForAction(2));
        Assert.Equal(1, TempleHealMenu.PartySlotForAction(3));
        Assert.Equal(2, TempleHealMenu.PartySlotForAction(4));
    }

    [Fact]
    public void OtherButtonsSelectNobody() {
        Assert.Equal(-1, TempleHealMenu.PartySlotForAction(TempleHealEntry.CureActionId));
        Assert.Equal(-1, TempleHealMenu.PartySlotForAction(TempleHealEntry.DoneActionId));
        Assert.Equal(-1, TempleHealMenu.PartySlotForAction(TempleHealEntry.NextActionId));
    }

    [Fact]
    public void RightClickingThePersonTellsYouAboutThePerson() {
        Assert.Equal(TempleHealMenu.CharacterDescriptionDialog,
            TempleHealMenu.HelpDialogFor(TempleHealEntry.NextActionId));
        Assert.Equal(TempleHealMenu.CharacterDescriptionDialog,
            TempleHealMenu.HelpDialogFor(TempleHealMenu.PortraitActionId));
        Assert.Equal(TempleHealMenu.ButtonHelpDialog,
            TempleHealMenu.HelpDialogFor(TempleHealEntry.CureActionId));
        Assert.Equal(TempleHealMenu.ButtonHelpDialog,
            TempleHealMenu.HelpDialogFor(TempleHealEntry.DoneActionId));
    }

    // ---- the shared roster helper ------------------------------------------------------------------

    [Fact]
    public void TheTwoScreensPortraitRowsShareOneArithmetic() {
        // Different bases, same computation — extracted rather than restated. The casting ring
        // starts at 128 and the healer at 2.
        Assert.Equal(1, ActiveParty.SlotForAction(3, firstActionId: 2));
        Assert.Equal(1, ActiveParty.SlotForAction(129, firstActionId: 128));
        Assert.Equal(-1, ActiveParty.SlotForAction(1, firstActionId: 2));
        Assert.Equal(-1, ActiveParty.SlotForAction(2 + ActiveParty.Slots, firstActionId: 2));
    }
}
