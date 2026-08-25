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
    public void TheSpellGateReadsAWORDAndAMask_NotACharacter() {
        // *** This was written wrong first. *** The original reads
        // characters[CHR_OWYN].spellsKnown[0] & 0x10 for key 71 and spellsKnown[2] & 0x200 for 106
        // — the CHARACTER is fixed in the code, so the case's First is the (one-based) WORD and
        // Second the mask. Reading First as a character id asks the wrong table.
        //
        // Key 71 => word 0, mask 0x10. Setting exactly that bit must WITHDRAW the topic.
        Assert.False(KeywordAvailability.Evaluate(
            71, 1, 0, Flags(), chapter: 1, spellsKnownWord: i => i == 0 ? 0x10 : 0).Available);
        Assert.True(KeywordAvailability.Evaluate(
            71, 1, 0, Flags(), chapter: 1, spellsKnownWord: i => 0).Available);

        // Key 106 => word 2, mask 0x200. The bit in the WRONG word must not gate it.
        Assert.True(KeywordAvailability.Evaluate(
            106, 1, 0, Flags(), chapter: 1, spellsKnownWord: i => i == 0 ? 0x200 : 0).Available);
        Assert.False(KeywordAvailability.Evaluate(
            106, 1, 0, Flags(), chapter: 1, spellsKnownWord: i => i == 2 ? 0x200 : 0).Available);

        Assert.Equal(KeywordAvailability.Requirement.SpellNotKnown,
            KeywordAvailability.Evaluate(71, 1, 0, Flags(), chapter: 1).Unevaluated);
    }

    [Fact]
    public void TheItemGatesAreResolvedAndTwoTopicsSHARETheirItem() {
        // Ids come from the literal operands of itemtbl_partySize_by_kind, not from matching the
        // symbol names: "Rations" is ambiguous in the object table (72 plain, 73 poisoned,
        // 74 spoiled, 134 "Days Rations") and only the disassembly says it is 72.
        Assert.Equal(0x48, KeywordAvailability.RationsObjectId);

        var asked = new List<int>();
        KeywordAvailability.Evaluate(76, 1, 0, Flags(), chapter: 1,
            partyCarriesItem: id => { asked.Add(id); return false; });
        KeywordAvailability.Evaluate(148, 1, 0, Flags(), chapter: 1,
            partyCarriesItem: id => { asked.Add(id); return false; });
        Assert.Equal(new[] { 0x48, 0x48 }, asked);

        // Carrying it WITHDRAWS the topic — the gate is "party lacks".
        Assert.False(KeywordAvailability.Evaluate(
            76, 1, 0, Flags(), chapter: 1, partyCarriesItem: _ => true).Available);
        Assert.True(KeywordAvailability.Evaluate(
            76, 1, 0, Flags(), chapter: 1, partyCarriesItem: _ => false).Available);
    }

    [Fact]
    public void TheItemHalfOfTheTwoFlagsAndItemGateIsStillUnresolved() {
        // Key 163's two parameters are already spent on its two flags, so there is nowhere to put
        // the object id. Refused rather than guessed.
        Assert.Equal(KeywordAvailability.Requirement.TwoFlagsAndItem,
            KeywordAvailability.Evaluate(163, 1, 0, Flags(), chapter: 1,
                partyCarriesItem: _ => false).Unevaluated);
    }

    [Fact]
    public void TheUnmodelledGatesStayRefusedRatherThanGuessed() {
        KeywordAvailability.Decision d = KeywordAvailability.Evaluate(
            17, ownFlagValue: 1, suppressedFlagValue: 0, Flags(), chapter: 1);

        Assert.Equal(KeywordAvailability.Requirement.Unmodelled, d.Unevaluated);
    }
}
