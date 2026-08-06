namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// An entry's own <c>SetTextVariable</c> ops, applied on top of the seeded table
/// (<c>dialog_cmbt_name_assign_kind</c> @0x48b66). The standing defaults themselves are
/// <see cref="DialogSlotSeedingTests"/>; this covers the kinds an entry names.
/// Spec: docs/specs/party-money-display.md §3.5-§3.7 for the money kinds.
/// </summary>
public class DialogSlotPopulatorTests {
    private static readonly string[] Names =
        { "Locklear", "Gorath", "Owyn", "Pug", "James", "Patrus" };

    private static DialogSlotContext Context(int money = 0, int quoted = 0) {
        int n = 0;
        return new DialogSlotContext {
            PartyRoster = new[] { 0, 1, 2 },
            ActorNames = Names,
            ChapterSpeakerId = 0,
            CurrentActorId = 0,
            PartyMoneyInRoyals = money,
            QuotedAmount = quoted,
            Random = bound => bound <= 0 ? 0 : n++ % bound,
        };
    }

    private static DialogEntry EntryWith(int slot, int source) => new() {
        Text = "@" + slot,
        Actions = new List<DialogActionBase> { new SetTextVariableAction { Slot = slot, Source = source } },
    };

    private static string Slot(int slot, int source, DialogSlotContext context) =>
        DialogSlotPopulator.BuildSlots(EntryWith(slot, source), context)[slot];

    // DDX 1800023, "That will be @1" — the shop's asking price.
    [Fact]
    public void Source19FormatsTheQuotedAmountAsProse() =>
        Assert.Equal("2 sovereigns and 5 royals", Slot(1, 19, Context(money: 1234, quoted: 25)));

    // The single shipped source-20 use (DIAL_Z22, "@4 found they had @2 to spend").
    [Fact]
    public void Source20FormatsThePartyPurseAsProse() =>
        Assert.Equal("123 sovereigns and 4 royals", Slot(2, 20, Context(money: 1234, quoted: 25)));

    // Regression: the purse used to be substituted as its raw royal count.
    [Fact]
    public void Source20IsNotTheRawRoyalCount() =>
        Assert.NotEqual("1234", Slot(2, 20, Context(money: 1234)));

    // DDX 1800004, "@ has @1 of @2 total health points" — same global, no money wording.
    [Fact]
    public void Source21IsABareNumber() =>
        Assert.Equal("25", Slot(1, 21, Context(quoted: 25)));

    [Fact]
    public void EmptyPurseReadsSingular() =>
        Assert.Equal("0 sovereign", Slot(0, 20, Context(money: 0)));

    // Kinds 1-6 name a specific party member, one-based.
    [Fact]
    public void SourcesOneToSixNameASpecificMember() {
        Assert.Equal("Locklear", Slot(1, 1, Context()));
        Assert.Equal("Owyn", Slot(1, 3, Context()));
    }

    // An unmodelled kind must not blank the slot — it keeps whatever the seeding put there, which
    // is a plausible name rather than an empty token.
    [Fact]
    public void UnmodelledKindKeepsTheSeededName() {
        DialogSlotContext context = Context();
        string seeded = DialogSlotPopulator.CreateForPlay(context).Names[4];
        Assert.False(string.IsNullOrEmpty(seeded));
        Assert.Equal(seeded, Slot(4, 17, context));
    }

    // An entry writes on top of the seeded table rather than replacing it: the slots it does not
    // name keep their standing defaults. This is what makes "@4 checked their funds" resolve when
    // the entry's only op targets slot 0.
    [Fact]
    public void EntryOpsLeaveTheOtherSeededSlotsIntact() {
        DialogSlotContext context = Context(quoted: 25);
        string[] slots = DialogSlotPopulator.BuildSlots(EntryWith(0, 19), context);
        Assert.Equal("2 sovereigns and 5 royals", slots[0]);
        Assert.Equal("Locklear", slots[4]); // the chapter speaker, still seeded
        Assert.False(string.IsNullOrEmpty(slots[3]));
        Assert.False(string.IsNullOrEmpty(slots[5]));
    }

    // The whole bug, end to end: DDX 1800034's text against a real slot table.
    [Fact]
    public void PartyFundsDialogResolvesBothItsTokens() {
        DialogSlotContext context = Context(money: 1234, quoted: 1234);
        var entry = new DialogEntry {
            Text = "@4 checked their funds. They had @0.",
            Actions = new List<DialogActionBase> {
                new SetTextVariableAction { Slot = 0, Source = 19 },
            },
        };
        string[] slots = DialogSlotPopulator.BuildSlots(entry, context);
        Assert.Equal("Locklear checked their funds. They had 123 sovereigns and 4 royals.",
            TextVariableResolver.Substitute(entry.Text, slots, context.NameOf(context.CurrentActorId)));
    }
}
