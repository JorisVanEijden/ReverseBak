namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using System.Collections.Generic;
using Xunit;

/// <summary>Applying the fifteen hand-written keyword gates.</summary>
public class KeywordAvailabilityEvaluateTests {
    private static System.Func<int, int> Flags(params (int Key, int Value)[] set) {
        var map = new Dictionary<int, int>();
        foreach ((int k, int v) in set) {
            map[k] = v;
        }
        return k => map.TryGetValue(k, out int v) ? v : 0;
    }

    [Fact]
    public void ATopicWithNoSpecialCaseJustTakesTheGeneralRule() {
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            globalKey: 999, ownFlagValue: 1, suppressedFlagValue: 0, Flags(), chapter: 1);

        Assert.True(d.Available);
        Assert.Null(d.Unevaluated);
    }

    [Fact]
    public void TheGateNARROWS_ItDoesNotGrant() {
        // Key 44 requires flag 8044. With the topic's own flag set but 8044 clear the topic is
        // WITHDRAWN — which is the whole point of applying the gates: without them these show.
        KeywordAvailability.Decision withoutIt = KeywordAvailability.Evaluate(
            44, ownFlagValue: 1, suppressedFlagValue: 0, Flags(), chapter: 1);
        Assert.False(withoutIt.Available);

        KeywordAvailability.Decision withIt = KeywordAvailability.Evaluate(
            44, ownFlagValue: 1, suppressedFlagValue: 0, Flags((8044, 1)), chapter: 1);
        Assert.True(withIt.Available);
    }

    [Fact]
    public void SUPPRESSIONHasTheLastWordEvenWhenTheGateSaysYes() {
        // Every path falls through the same tail check. Treating a special case as an override is
        // how retired topics come back.
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            44, ownFlagValue: 1, suppressedFlagValue: 1, Flags((8044, 1)), chapter: 1);

        Assert.False(d.Available);
    }

    [Fact]
    public void ARedirectREPLACESTheFlagTheGeneralRuleTests() {
        // Key 130 redirects to 56222. Its OWN flag is clear here, and it is still offered — the
        // redirect is a different shape from the twelve that narrow, and folding it in as one more
        // "and" would hide it.
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            130, ownFlagValue: 0, suppressedFlagValue: 0, Flags((56222, 1)), chapter: 1);

        Assert.True(d.Available);
        Assert.Null(d.Unevaluated);
    }

    [Fact]
    public void EitherOfTwoFlagsIsEnough() {
        Assert.True(KeywordAvailability.Evaluate(
            132, 1, 0, Flags((51021, 1)), chapter: 1).Available);
        Assert.True(KeywordAvailability.Evaluate(
            132, 1, 0, Flags((6521, 1)), chapter: 1).Available);
        Assert.False(KeywordAvailability.Evaluate(
            132, 1, 0, Flags(), chapter: 1).Available);
    }

    [Fact]
    public void TheChapterGateComparesExactly() {
        Assert.True(KeywordAvailability.Evaluate(117, 1, 0, Flags(), chapter: 6).Available);
        Assert.False(KeywordAvailability.Evaluate(117, 1, 0, Flags(), chapter: 5).Available);
        Assert.False(KeywordAvailability.Evaluate(117, 1, 0, Flags(), chapter: 7).Available);
    }

    [Fact]
    public void AnUnevaluableGateFallsBackAndSAYSSo() {
        // *** The honest half. *** The item gates carry object ids by SYMBOL NAME, unresolved, so
        // there is nothing to ask. The answer reverts to the general rule and reports which gate it
        // could not apply — rather than returning a bare bool that reads as complete.
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            9, ownFlagValue: 1, suppressedFlagValue: 0, Flags(), chapter: 1);

        Assert.True(d.Available);
        Assert.Equal(KeywordAvailability.Requirement.PartyLacksItem, d.Unevaluated);
    }

    [Fact]
    public void ASpellGateIsAppliedWhenTheLookupIsSupplied() {
        // Key 71: Owyn must NOT already know the spell.
        Assert.False(KeywordAvailability.Evaluate(
            71, 1, 0, Flags(), chapter: 1, knowsSpell: (c, s) => true).Available);
        Assert.True(KeywordAvailability.Evaluate(
            71, 1, 0, Flags(), chapter: 1, knowsSpell: (c, s) => false).Available);
        // And reports itself unevaluated when no lookup is given.
        Assert.Equal(KeywordAvailability.Requirement.SpellNotKnown,
            KeywordAvailability.Evaluate(71, 1, 0, Flags(), chapter: 1).Unevaluated);
    }

    [Fact]
    public void TheUnmodelledGatesStayRefusedRatherThanGuessed() {
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            17, ownFlagValue: 1, suppressedFlagValue: 0, Flags(), chapter: 1);

        Assert.Equal(KeywordAvailability.Requirement.Unmodelled, d.Unevaluated);
    }
}
