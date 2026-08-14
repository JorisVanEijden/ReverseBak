namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The "asked about:" prompt and what it answers with. The farewell sharing the dismiss code is
/// what makes the loop as small as it is.
/// </summary>
public class KeywordPromptTests {
    [Fact]
    public void ThePromptIsTheSpeakersNameAndALiteralSuffix() {
        Assert.Equal("Gorath asked about:", KeywordPrompt.PromptFor("Gorath"));
    }

    [Fact]
    public void TheNameGoesInVerbatim() {
        // Straight concatenation — no placeholder substitution, no punctuation beyond the suffix.
        Assert.StartsWith("Owyn", KeywordPrompt.PromptFor("Owyn"));
        Assert.EndsWith(KeywordPrompt.PromptSuffix, KeywordPrompt.PromptFor("Owyn"));
    }

    [Fact]
    public void NoTopicsMeansNoPromptEither() {
        // It builds the grid first and gives up — so an NPC with nothing to say shows no heading,
        // rather than an empty box under one.
        Assert.False(KeywordPrompt.Appears(0));
        Assert.True(KeywordPrompt.Appears(1));
    }

    [Fact]
    public void ThatIsTheSameConditionTheGridItselfUses() {
        for (var available = 0; available < 4; available++) {
            Assert.Equal(KeywordMenu.Opens(available), KeywordPrompt.Appears(available));
        }
    }

    [Fact]
    public void ChoosingTheFarewellAndDismissingArTheSamePath() {
        // Which is why the farewell's action id is 1 — 1 is the dismiss code.
        Assert.Equal(DialogChoiceMenu.DismissedResult, KeywordMenu.FarewellActionId);
        Assert.True(KeywordPrompt.EndsTheConversation(KeywordMenu.FarewellActionId));
    }

    [Fact]
    public void DismissingAnswersWithNothingChosen() {
        Assert.Equal(KeywordPrompt.NothingChosen,
            KeywordPrompt.Result(DialogChoiceMenu.DismissedResult));
    }

    [Fact]
    public void AChosenTopicAnswersWithItsBranchIndex() {
        Assert.Equal(0, KeywordPrompt.Result(KeywordMenu.ActionIdFor(0)));
        Assert.Equal(4, KeywordPrompt.Result(KeywordMenu.ActionIdFor(4)));
    }

    [Fact]
    public void TheResultAgreesWithTheOtherMenusIndexDecoding() {
        for (var branch = 0; branch < 5; branch++) {
            Assert.Equal(DialogChoiceMenu.EntryIndexOf(KeywordMenu.ActionIdFor(branch)),
                KeywordPrompt.Result(KeywordMenu.ActionIdFor(branch)));
        }
    }

    [Fact]
    public void AnythingBelowTheEntryBaseIsNotASelection() {
        Assert.Equal(KeywordPrompt.NothingChosen, KeywordPrompt.Result(0));
        Assert.Equal(KeywordPrompt.NothingChosen, KeywordPrompt.Result(0x7f));
    }
}
