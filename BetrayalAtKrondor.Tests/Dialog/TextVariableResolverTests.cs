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
}
