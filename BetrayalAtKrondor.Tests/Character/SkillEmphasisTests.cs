namespace BetrayalAtKrondor.Tests.Character;

using GameData;
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

    // --- the study rate: what the mark is FOR (TASK-430) -------------------------------------

    /// <summary>A reader over a set of marked attributes for one actor.</summary>
    private static System.Func<int, int> Marks(int actorNumber, params int[] attributes) =>
        key => {
            foreach (int attribute in attributes) {
                if (key == SkillEmphasis.FlagFor(actorNumber, attribute)) {
                    return 1;
                }
            }
            return 0;
        };

    [Theory]
    [InlineData(0, 0)]     // nothing marked: no rate at all
    [InlineData(1, 26)]    // 0x1a / 1
    [InlineData(2, 13)]
    [InlineData(3, 8)]     // integer division, not 8.67
    [InlineData(4, 6)]
    [InlineData(26, 1)]
    [InlineData(27, 0)]    // past the numerator it floors to nothing
    public void TheRateIs26OverTheNumberMarked(int marked, int expected) =>
        Assert.Equal(expected, SkillEmphasis.TrainRate(marked));

    [Fact]
    public void OneMarkedRatingAdvancesFiftyPercentFaster() {
        // The walkthrough's "if it is the ONLY Skill Selected it will rise 50% faster" is
        // TrainRate over StatEngine's divisor of 52: 26/52 = +50%.
        var stat = new ActorStat { Base = 10, Max = 100 };
        var control = new ActorStat { Base = 10, Max = 100 };
        const long delta = 0x400;

        StatEngine.Modify(control, ActorAttribute.LockPicking, delta);
        StatEngine.Modify(stat, ActorAttribute.LockPicking, delta,
            StatChangeMode.Absolute, SkillEmphasis.TrainRate(1));

        Assert.Equal(10 + 4, control.Base);
        Assert.Equal(10 + 6, stat.Base);   // 4 * 1.5
    }

    [Fact]
    public void TheRateIsPerActorButTheMarkIsPerRating() {
        // Emphasising Lockpicking must not speed up Barding: STAT.C:271 asks for THIS rating's own
        // flag before applying the actor's rate.
        System.Func<int, int> marks = Marks(2, (int)ActorAttribute.LockPicking);

        Assert.Equal(26, SkillEmphasis.BonusFor(marks, 2, (int)ActorAttribute.LockPicking, true));
        Assert.Equal(0, SkillEmphasis.BonusFor(marks, 2, (int)ActorAttribute.Barding, true));
    }

    [Fact]
    public void ASecondMarkHalvesTheBonusOnTheFirst() {
        System.Func<int, int> one = Marks(0, (int)ActorAttribute.Barding);
        System.Func<int, int> two = Marks(0, (int)ActorAttribute.Barding, (int)ActorAttribute.Haggling);

        Assert.Equal(26, SkillEmphasis.BonusFor(one, 0, (int)ActorAttribute.Barding, true));
        Assert.Equal(13, SkillEmphasis.BonusFor(two, 0, (int)ActorAttribute.Barding, true));
    }

    [Fact]
    public void ANonPartyActorGetsNothingHoweverItsFlagsRead() =>
        // STAT.C:271 gates the whole bonus on charSlot != 0.
        Assert.Equal(0, SkillEmphasis.BonusFor(Marks(1, (int)ActorAttribute.AccuracyMelee), 1,
            (int)ActorAttribute.AccuracyMelee, isPartyMember: false));

    [Fact]
    public void TheSeventeenthSlotIsAddressableAndNeverCounted() {
        // charscreen_recalc_train_rates counts i < 0x10 while indexing memberIdx * 0x11 + i, so the
        // combo pseudo-attribute can hold a flag and must not dilute the rate.
        System.Func<int, int> marks = Marks(0, (int)ActorAttribute.Barding, SkillEmphasis.CountedAttributes);

        Assert.Equal(1, SkillEmphasis.EmphasisedCount(marks, 0));
        Assert.Equal(26, SkillEmphasis.BonusFor(marks, 0, (int)ActorAttribute.Barding, true));
    }

    [Fact]
    public void NoReaderMeansNoBonusRatherThanAThrow() =>
        Assert.Equal(0, SkillEmphasis.BonusFor(null, 0, (int)ActorAttribute.Barding, true));
}
