namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Right-click describe records for the combat and shoot menus.</summary>
public class CombatHelpTests {
    [Fact]
    public void TheRecordsRunInSWITCHOrder_NotActionIdOrder() {
        // *** The trap. *** Deriving a record from the action number would land on the wrong text for
        // almost every button: 0xfe..0x10d walks 2,3,4,5,6,8,9,7,50,19,31,46,32,47,30,33.
        Assert.Equal(0xfe, CombatHelp.DialogFor(2));
        Assert.Equal(0x105, CombatHelp.DialogFor(7));   // NOT 0x103 - kind order puts 7 last
        Assert.Equal(0x103, CombatHelp.DialogFor(8));
        Assert.Equal(0x10d, CombatHelp.DialogFor(33));
        Assert.Equal(16, CombatHelp.Count);
    }

    [Fact]
    public void TheQuarrelRunMatchesTheKindTable() {
        // Reached independently: the help records walk the quarrel buttons in the same out-of-order
        // sequence CombatMenuSlots derived from the rebuild routine. Corroboration, not a copy.
        int expected = CombatHelp.FirstRecord;
        foreach (int actionId in CombatMenuSlots.ActionIdByQuarrelKind) {
            Assert.Equal(expected, CombatHelp.DialogFor(actionId));
            expected++;
        }
    }

    [Fact]
    public void TheMeleeButtonsHaveTheirOwnRecords() {
        Assert.Equal(0x107, CombatHelp.DialogFor(CombatCommands.DefendId));
        Assert.Equal(0x108, CombatHelp.DialogFor(CombatCommands.ShootId));
        Assert.Equal(0x109, CombatHelp.DialogFor(CombatCommands.CastId));
        Assert.Equal(0x10c, CombatHelp.DialogFor(CombatCommands.AutoResolveId));
        Assert.Equal(0x10d, CombatHelp.DialogFor(CombatCommands.BackOrRetreatId));
    }

    [Fact]
    public void TheTwoButtonsWithoutRecordsAreTheOnesYouCannotClick() {
        // 14 is the disabled label - drawn, never clickable, so never right-clickable either.
        // 22 is the hidden character-screen zone, shipped Visible=False. Neither is in the switch.
        Assert.False(CombatHelp.HasDialog(CombatCommands.CapabilityLabelId));
        Assert.False(CombatHelp.HasDialog(CombatCommands.CharacterScreenId));
        Assert.Equal(CombatHelp.None, CombatHelp.DialogFor(14));
    }

    [Fact]
    public void AnUnknownIdHasNoRecord() {
        Assert.Equal(-1, CombatHelp.DialogFor(9999));
    }
}
