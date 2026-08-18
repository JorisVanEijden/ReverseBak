namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// Marking a rating for study — the character sheet's per-skill emphasis
/// (<c>charscreen_info_loop</c> @0x58378).
/// </summary>
public class SkillEmphasisTests {
    [Fact]
    public void TheMarkOnABarIsThisFlagAndNotAnotherOne() {
        // Two per-attribute arrays with the same stride sit 120 apart: this one, and the
        // "changed since you last looked" flags. Reading one for the other would make every
        // improvement look like a study choice.
        Assert.NotEqual(CharacterSheetRow.ChangedFlagBase, SkillEmphasis.FlagBase);
        Assert.Equal(CharacterSheetRow.AttributesPerActor, SkillEmphasis.AttributesPerActor);
        Assert.NotEqual(CharacterSheetRow.ChangedFlagFor(2, 11), SkillEmphasis.FlagFor(2, 11));
    }

    [Fact]
    public void EachActorsMarksAreTheirOwn() =>
        Assert.Equal(SkillEmphasis.AttributesPerActor,
            SkillEmphasis.FlagFor(1, 0) - SkillEmphasis.FlagFor(0, 0));

    [Fact]
    public void ClickingIsAPlainToggle() {
        Assert.Equal(1, SkillEmphasis.Toggled(0));
        Assert.Equal(0, SkillEmphasis.Toggled(1));
        // A flag a save left holding something else comes back as 1, not incremented.
        Assert.Equal(0, SkillEmphasis.Toggled(7));
        Assert.True(SkillEmphasis.IsEmphasised(7));
    }

    [Fact]
    public void ARatingTheCharacterNeverHadCannotBeStudied() {
        // Tested on the MAXIMUM — the same "never had it" case that prints N/A rather than a
        // percentage — and the click is simply dropped.
        Assert.False(SkillEmphasis.CanEmphasise(0));
        Assert.True(SkillEmphasis.CanEmphasise(1));
    }

    [Fact]
    public void ARowMeansOneThingToTheToggleAndAnotherToTheHelp() {
        // The toggle wants the ATTRIBUTE and the help wants the ROW, which is why the original
        // adds -124 in one arm and -128 in the other.
        Assert.Equal(0, SkillEmphasis.RowForAction(SkillEmphasis.FirstRowActionId));
        Assert.Equal(CharacterSheetLayout.LowerHalfFirstAttribute,
            SkillEmphasis.AttributeForRow(SkillEmphasis.RowForAction(SkillEmphasis.FirstRowActionId)));
    }

    [Fact]
    public void TheRowsCoverTheLowerHalfAndNothingElse() {
        int last = SkillEmphasis.FirstRowActionId + CharacterSheetLayout.LowerHalfAttributeCount - 1;

        Assert.Equal(CharacterSheetRow.DisplayableAttributes - 1,
            SkillEmphasis.AttributeForRow(SkillEmphasis.RowForAction(last)));
        Assert.Equal(-1, SkillEmphasis.RowForAction(last + 1));
        Assert.Equal(-1, SkillEmphasis.RowForAction(SkillEmphasis.FirstRowActionId - 1));
    }
}
