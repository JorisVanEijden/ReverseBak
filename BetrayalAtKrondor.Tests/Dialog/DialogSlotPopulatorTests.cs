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
[Collection(BetrayalAtKrondor.Tests.Text.UiStringsCollection.Name)]
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
            AttributeValueOf = _ => 0,
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

    // An unmodelled kind (32 = the "ask about" name/keyword lookup, not yet ported) must not blank
    // the slot — it keeps whatever the seeding put there, which is a plausible name rather than an
    // empty token. (Kind 27 used to be this test's example; it is modelled as of this change.)
    [Fact]
    public void UnmodelledKindKeepsTheSeededName() {
        DialogSlotContext context = Context();
        string seeded = DialogSlotPopulator.CreateForPlay(context).Names[4];
        Assert.False(string.IsNullOrEmpty(seeded));
        Assert.Equal(seeded, Slot(4, 32, context));
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

    // ---- the actor kinds (globals 30004 / +0x0598 / +0x059A) -----------------------------

    [Fact]
    public void Kinds10And30NameThePrimaryActor() {
        DialogSlotContext context = Context();
        context.PrimaryActorId = 4;
        Assert.Equal("James", Slot(1, 10, context));
        Assert.Equal("James", Slot(1, 30, context));
    }

    [Fact]
    public void Kind11NamesTheSecondaryActor() {
        DialogSlotContext context = Context();
        context.SecondaryActorId = 5;
        Assert.Equal("Patrus", Slot(1, 11, context));
    }

    [Fact]
    public void Kind12NamesTheTertiaryActor() {
        DialogSlotContext context = Context();
        context.TertiaryActorId = 3;
        Assert.Equal("Pug", Slot(1, 12, context));
    }

    // ---- creature and object names ---------------------------------------------------------

    // 72 shipped entries use kind 17 — the most-used kind of all.
    [Fact]
    public void Kind17NamesTheCreatureAndMarksTheSlotAsOne() {
        DialogSlotContext context = Context();
        context.CreatureType = 7;
        context.CreatureNameOf = id => id == 7 ? "Wraith" : "?";
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(context);
        DialogSlotPopulator.Assign(table, 1, 17, 0, context);
        Assert.Equal("Wraith", table.Names[1]);
        // The mark is what turns on the renderer's article/possessive rules for this token.
        Assert.Equal(DialogSlotTable.CreatureActor, table.Kinds[1]);
    }

    [Fact]
    public void Kind18NamesTheKeyObject() {
        DialogSlotContext context = Context();
        context.KeyObjectId = 12;
        context.ObjectNameOf = id => id == 12 ? "Bowstring" : "?";
        Assert.Equal("Bowstring", Slot(1, 18, context));
    }

    [Fact]
    public void MissingNameLookupsYieldEmptyRatherThanThrowing() {
        DialogSlotContext context = Context();
        Assert.Equal("", Slot(1, 17, context));
        Assert.Equal("", Slot(1, 18, context));
    }

    // ---- the remaining number and label kinds ----------------------------------------------

    [Fact]
    public void Kinds22And23AreBareNumbers() {
        DialogSlotContext context = Context();
        context.Global30015 = 42;
        context.Global30018 = 7;
        Assert.Equal("42", Slot(1, 22, context));
        Assert.Equal("7", Slot(1, 23, context));
    }

    [Fact]
    public void Kind28PicksTheKeeperByEncounterType() {
        DialogSlotContext shop = Context();
        Assert.Equal("shopkeeper", Slot(1, 28, shop));
        DialogSlotContext inn = Context();
        inn.IsRestEncounter = true;
        Assert.Equal("tavernkeeper", Slot(1, 28, inn));
    }

    // DIAL_Z24: "My @0 rating is limited to a mere @1" — the ONE entry in the whole corpus
    // with an unresolvable token before this. Kind 27 writes BOTH slots.
    [Fact]
    public void Kind27WritesTheAttributeNameAndItsValue() {
        DialogSlotContext context = Context();
        context.Global30015 = 3;               // attribute index -> Strength
        context.AttributeValueOf = _ => 42;
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(context);
        DialogSlotPopulator.Assign(table, 0, 27, 0, context);
        Assert.Equal("Strength", table.Names[0]);
        Assert.Equal("42", table.Names[1]);
    }

    // DIAL_Z21 #2100016: "The party's @1 ability has increased." Kind 29 reads global 30018.
    [Fact]
    public void Kind29NamesTheAttributeFromGlobal30018() {
        DialogSlotContext context = Context();
        context.Global30018 = 13;              // -> Lockpick
        Assert.Equal("Lockpick", Slot(1, 29, context));
    }

    // The engine hardcodes both kind-27 destinations (DIALOG.C:531-535: `g_speaker_names[0]` and
    // `g_speaker_names[1]` are literals) — the requested slot is ignored entirely. Request slot 3
    // to prove the write still lands on 0/1 and that slot 3 is untouched, rather than clobbered.
    [Fact]
    public void Kind27IgnoresTheRequestedSlotAndAlwaysWritesZeroAndOne() {
        DialogSlotContext context = Context();
        context.Global30015 = 3;               // attribute index -> Strength
        context.AttributeValueOf = _ => 42;
        DialogSlotTable table = DialogSlotPopulator.CreateForPlay(context);
        table.Names[3] = "sentinel";

        DialogSlotPopulator.Assign(table, 3, 27, 0, context);

        Assert.Equal("Strength", table.Names[0]);
        Assert.Equal("42", table.Names[1]);
        Assert.Equal("sentinel", table.Names[3]);
    }
}
