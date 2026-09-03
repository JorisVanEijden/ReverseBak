namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog.Actions;
using Xunit;

/// <summary>
/// The two-roll wager — <c>PerformSubAction</c> case 8.
/// </summary>
public class DialogWagerTests {
    [Fact]
    public void ADrawSettlesNOTHING() {
        // *** The one that had to come from the disassembly. *** canassa tests the same condition
        // twice, so its third arm cannot run; the binary really does compare a third time with jl
        // after the second arm's jge (0x410c8 vs 0x4109b), reaches only equality, and falls to the
        // return. So a draw leaves the outcome global holding the LAST wager's value — which is
        // why this reports "not settled" rather than a third outcome.
        DialogWager.Result draw = DialogWager.Settle(
            partyRoll: 5, houseRoll: 5, quotedPrice: 100, winPercent: 50, fund: 30);

        Assert.False(draw.Settled);
        Assert.Equal(0, draw.GoldDelta);
        Assert.Equal(30, draw.Fund);
    }

    [Fact]
    public void AWinPaysAPercentageOfTheStakeAndTakesItOffTheHouse() {
        DialogWager.Result win = DialogWager.Settle(
            partyRoll: 9, houseRoll: 2, quotedPrice: 200, winPercent: 50, fund: 300);

        Assert.True(win.Settled);
        Assert.Equal(DialogWager.PartyWins, win.Outcome);
        Assert.Equal(100, win.GoldDelta);
        Assert.Equal(200, win.Fund);
    }

    [Fact]
    public void AHouseThatCannotCoverTheWinIsFLOOREDAtZeroAndThePartyIsStillPaidInFull() {
        // `if (fund < payout) fund = 0; else fund -= payout;` — the payout is NOT reduced to what
        // the house has. The party's purse and the house's fund are settled independently.
        DialogWager.Result win = DialogWager.Settle(
            partyRoll: 9, houseRoll: 2, quotedPrice: 200, winPercent: 100, fund: 30);

        Assert.Equal(200, win.GoldDelta);
        Assert.Equal(0, win.Fund);
    }

    [Fact]
    public void ALossPaysTheWholeStakeAndPutsItOnTheHouse() {
        DialogWager.Result loss = DialogWager.Settle(
            partyRoll: 1, houseRoll: 7, quotedPrice: 200, winPercent: 50, fund: 300);

        Assert.True(loss.Settled);
        Assert.Equal(DialogWager.PartyLoses, loss.Outcome);
        Assert.Equal(-200, loss.GoldDelta);
        Assert.Equal(500, loss.Fund);
    }

    [Fact]
    public void PastTheCeilingALossAddsNOTHINGToTheFund() {
        // A ceiling on the ADD, not a clamp on the result — the fund stays exactly where it was.
        DialogWager.Result loss = DialogWager.Settle(
            partyRoll: 1, houseRoll: 7, quotedPrice: 200, winPercent: 50,
            fund: DialogWager.FundCeiling);

        Assert.Equal(-200, loss.GoldDelta);
        Assert.Equal(DialogWager.FundCeiling, loss.Fund);
        // One below the line still takes the whole stake, so the ceiling can be crossed but not
        // approached from above.
        Assert.Equal(DialogWager.FundCeiling + 199, DialogWager.Settle(
            1, 7, 200, 50, DialogWager.FundCeiling - 1).Fund);
    }

    [Fact]
    public void TheDieIsATwelveBitDrawTakenModuloItsSides() {
        Assert.Equal(3, DialogWager.RollDie(twelveBitRoll: 4095, sides: 6));
        Assert.Equal(0, DialogWager.RollDie(twelveBitRoll: 0, sides: 6));

        // A zero-sided die would trap the original's `div`. Answering 0 keeps a badly authored
        // dialog from taking the game down.
        Assert.Equal(0, DialogWager.RollDie(twelveBitRoll: 100, sides: 0));
    }
}
