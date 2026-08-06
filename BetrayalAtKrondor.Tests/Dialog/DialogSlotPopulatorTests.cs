namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// <c>PopulateDialogSlotText</c> @0x48b66 cases 19/20/21 — the money sources.
/// Spec: docs/specs/party-money-display.md §3.5-§3.7.
/// </summary>
public class DialogSlotPopulatorTests {
    private static readonly IReadOnlyList<string> Party = new[] { "Locklear", "Gorath", "Owyn" };

    private static DialogEntry EntryWith(int slot, int source) => new() {
        Text = "@" + slot,
        Actions = new List<DialogActionBase> { new SetTextVariableAction { Slot = slot, Source = source } },
    };

    // DDX 1800023, "That will be @1" — the shop's asking price.
    [Fact]
    public void Source19FormatsTheQuotedAmountAsProse() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(1, 19), Party,
            partyMoneyInRoyals: 1234, quotedAmount: 25);
        Assert.Equal("2 sovereigns and 5 royals", slots[1]);
    }

    // The single shipped source-20 use (DIAL_Z22, "@4 found they had @2 to spend").
    [Fact]
    public void Source20FormatsThePartyPurseAsProse() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(2, 20), Party,
            partyMoneyInRoyals: 1234, quotedAmount: 25);
        Assert.Equal("123 sovereigns and 4 royals", slots[2]);
    }

    // Regression: the purse used to be substituted as its raw royal count.
    [Fact]
    public void Source20IsNotTheRawRoyalCount() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(2, 20), Party,
            partyMoneyInRoyals: 1234, quotedAmount: 0);
        Assert.NotEqual("1234", slots[2]);
    }

    // DDX 1800004, "@ has @1 of @2 total health points" — same global, no money wording.
    [Fact]
    public void Source21IsABareNumber() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(1, 21), Party,
            partyMoneyInRoyals: 0, quotedAmount: 25);
        Assert.Equal("25", slots[1]);
    }

    [Fact]
    public void EmptyPurseReadsSingular() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(0, 20), Party,
            partyMoneyInRoyals: 0, quotedAmount: 0);
        Assert.Equal("0 sovereign", slots[0]);
    }

    [Fact]
    public void UnmodelledSourceKeepsThePartyNameDefault() {
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(0, 17), Party,
            partyMoneyInRoyals: 50, quotedAmount: 10);
        Assert.Equal("Locklear", slots[0]);
    }
}
