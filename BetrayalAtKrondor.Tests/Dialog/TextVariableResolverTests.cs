namespace BetrayalAtKrondor.Tests.Dialog;
using GameData.Resources.Dialog;
using System.Collections.Generic;
using Xunit;

public class TextVariableResolverTests {
    private static readonly IReadOnlyList<string> Party = new[] { "Locklear", "Gorath", "Owyn", "", "", "" };

    [Fact] public void SubstitutesSlotZero() =>
        Assert.Equal("Locklear frowned.", TextVariableResolver.Substitute("@0 frowned.", Party));

    [Fact] public void SubstitutesMultiple() =>
        Assert.Equal("Gorath and Owyn", TextVariableResolver.Substitute("@1 and @2", Party));

    [Fact] public void PossessiveS() =>
        Assert.Equal("Locklears sword", TextVariableResolver.Substitute("@0s sword", Party));

    [Fact] public void OutOfRangeLeftVerbatim() =>
        Assert.Equal("@8", TextVariableResolver.Substitute("@8", Party));

    // The bug this class was reported for: DDX 1800034's "@4 checked their funds" showed the token.
    // The engine substitutes an empty slot as nothing — the token never survives to the screen.
    [Fact] public void EmptySlotContributesNothing() =>
        Assert.Equal(" checked their funds.",
            TextVariableResolver.Substitute("@4 checked their funds.", Party));

    // A bare '@' is a real token used across the DDX files ("@ gaped in astonishment"), and only
    // the '@' is consumed — the character after it is ordinary text.
    [Fact] public void BareAtBecomesTheCurrentActor() =>
        Assert.Equal("Gorath gaped in astonishment",
            TextVariableResolver.Substitute("@ gaped in astonishment", Party, "Gorath"));

    [Fact] public void BareAtKeepsTheFollowingCharacter() =>
        Assert.Equal("Gorath's lack of mastery",
            TextVariableResolver.Substitute("@'s lack of mastery", Party, "Gorath"));

    // Without an actor to name, leave the token visible rather than silently deleting it and
    // changing the sentence.
    [Fact] public void BareAtWithoutAnActorIsLeftVerbatim() =>
        Assert.Equal("email@host", TextVariableResolver.Substitute("email@host", Party));

    // ---- creature slots (kind 17) ----------------------------------------------------------
    // These rules key off the slot's KIND, so they need a table rather than a bare name list.

    private static DialogSlotTable CreatureIn(int slot, string name) {
        var table = new DialogSlotTable();
        for (int i = 0; i < DialogSlotTable.SlotCount; i++) {
            table.Names[i] = Party[i];
        }
        table.Names[slot] = name;
        table.Kinds[slot] = DialogSlotTable.CreatureActor;
        return table;
    }

    [Fact] public void CreatureNameFixesThePrecedingArticle() =>
        Assert.Equal("an Owl attacked", TextVariableResolver.Substitute("a @0 attacked", CreatureIn(0, "Owl")));

    [Fact] public void ArticleIsOnlyFixedForAOrO() =>
        Assert.Equal("a Goblin attacked",
            TextVariableResolver.Substitute("a @0 attacked", CreatureIn(0, "Goblin")));

    [Fact] public void ArticleIsNotFixedForAPartyMemberSlot() =>
        // Same sentence shape, ordinary slot: no rewriting.
        Assert.Equal("a Locklear attacked", TextVariableResolver.Substitute("a @0 attacked", Party));

    // The engine checks only "the second-to-last character emitted is an 'a'", never that the one
    // between is a space — so a word ending in "a " triggers it too. Faithful, quirk and all.
    [Fact] public void ArticleFixMisfiresAfterAnyWordEndingInA() =>
        Assert.Equal("Annan Owl", TextVariableResolver.Substitute("Anna @0", CreatureIn(0, "Owl")));

    [Fact] public void CreaturePossessiveReshapesTheTail() {
        Assert.Equal("Wraithes lair", TextVariableResolver.Substitute("@0s lair", CreatureIn(0, "Wraith")));
        Assert.Equal("Harpies lair", TextVariableResolver.Substitute("@0s lair", CreatureIn(0, "Harpy")));
        Assert.Equal("Goblins lair", TextVariableResolver.Substitute("@0s lair", CreatureIn(0, "Goblin")));
    }

    [Fact] public void PartyMemberPossessiveIsNotReshaped() =>
        // "Wraith" as a party member's name would keep the plain 's'.
        Assert.Equal("Locklears sword", TextVariableResolver.Substitute("@0s sword", Party));
}
