namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The gate at the top of <c>charscreen_temple_heal_menu</c> @0x5877e. What carries: a temple treats
/// afflictions, not injuries, and it has two different things to say about it.
/// </summary>
public class TempleHealEntryTests {
    private const int OtherMode = 1;

    [Fact]
    public void AnAfflictedPartyAlwaysGetsTheScreen() =>
        Assert.Equal(TempleHealOpening.Screen,
            TempleHealEntry.Decide(anyoneAfflicted: true, anyoneWounded: false, OtherMode));

    [Fact]
    public void TheModeIsNeverConsultedForAnAfflictedParty() {
        // The first test short-circuits before the mode is read, so every mode behaves alike here.
        // A port that checked the mode first would make some temples refuse the genuinely cursed.
        Assert.Equal(
            TempleHealEntry.Decide(anyoneAfflicted: true, anyoneWounded: true, OtherMode),
            TempleHealEntry.Decide(anyoneAfflicted: true, anyoneWounded: true,
                TempleHealEntry.ModeThatTreatsWounds));
    }

    [Fact]
    public void AHealthyPartyIsTurnedAwayWithNothingWrong() =>
        Assert.Equal(TempleHealOpening.NothingIsWrong,
            TempleHealEntry.Decide(anyoneAfflicted: false, anyoneWounded: false,
                TempleHealEntry.ModeThatTreatsWounds));

    [Fact]
    public void MerelyWoundedIsADifferentRefusal() =>
        // Not "nothing is wrong" — the priest says wounds heal with time or a chirurgeon, and that
        // he mends only spiritual things. Collapsing the two loses the line that says where to go.
        Assert.Equal(TempleHealOpening.WoundsAreNotOurBusiness,
            TempleHealEntry.Decide(anyoneAfflicted: false, anyoneWounded: true, OtherMode));

    [Fact]
    public void OneModeTreatsWoundsToo() =>
        Assert.Equal(TempleHealOpening.Screen,
            TempleHealEntry.Decide(anyoneAfflicted: false, anyoneWounded: true,
                TempleHealEntry.ModeThatTreatsWounds));

    [Fact]
    public void TheModeOnlyMattersForTheMerelyWounded() {
        // The single case the two modes disagree on — worth pinning, because it is the entire
        // observable difference between them.
        Assert.NotEqual(
            TempleHealEntry.Decide(false, anyoneWounded: true, OtherMode),
            TempleHealEntry.Decide(false, anyoneWounded: true, TempleHealEntry.ModeThatTreatsWounds));

        Assert.Equal(
            TempleHealEntry.Decide(false, anyoneWounded: false, OtherMode),
            TempleHealEntry.Decide(false, anyoneWounded: false, TempleHealEntry.ModeThatTreatsWounds));
    }

    [Fact]
    public void EachRefusalHasItsOwnDialogAndTheScreenHasNone() {
        Assert.Equal(TempleHealEntry.NothingIsWrongDialog,
            TempleHealEntry.DialogFor(TempleHealOpening.NothingIsWrong));
        Assert.Equal(TempleHealEntry.WoundsAreNotOurBusinessDialog,
            TempleHealEntry.DialogFor(TempleHealOpening.WoundsAreNotOurBusiness));
        Assert.Equal(0, TempleHealEntry.DialogFor(TempleHealOpening.Screen));

        Assert.NotEqual(TempleHealEntry.NothingIsWrongDialog,
            TempleHealEntry.WoundsAreNotOurBusinessDialog);
    }

    [Fact]
    public void ThePortraitAdvancesLikeTheNextButton() =>
        // REQ_HEAL ships an invisible click area over the portrait carrying the same action id.
        Assert.Equal(49, TempleHealEntry.NextActionId);

    [Fact]
    public void TheThreeButtonsAreDistinct() {
        Assert.NotEqual(TempleHealEntry.CureActionId, TempleHealEntry.NextActionId);
        Assert.NotEqual(TempleHealEntry.CureActionId, TempleHealEntry.DoneActionId);
        Assert.NotEqual(TempleHealEntry.NextActionId, TempleHealEntry.DoneActionId);
    }
}
