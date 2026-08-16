namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

public class CastMenuButtonsTests {
    [Fact]
    public void TheSixSchoolButtonsAreActionsTwoToSeven() {
        Assert.Equal(0, CastMenuSelection.SchoolForAction(2));
        Assert.Equal(5, CastMenuSelection.SchoolForAction(7));
        // Exit is not a school, and neither is anything past the sixth.
        Assert.Equal(-1, CastMenuSelection.SchoolForAction(CastMenuSelection.ExitActionId));
        Assert.Equal(-1, CastMenuSelection.SchoolForAction(8));
    }

    [Fact]
    public void ThePartySlotsAreTheThreeClickAreasFrom128() {
        Assert.Equal(0, CastMenuSelection.PartySlotForAction(128));
        Assert.Equal(2, CastMenuSelection.PartySlotForAction(130));
        Assert.Equal(-1, CastMenuSelection.PartySlotForAction(131));
        Assert.Equal(-1, CastMenuSelection.PartySlotForAction(7));
    }

    [Fact]
    public void EachButtonKindHasItsOwnHelpText() {
        // Distinct, because right-clicking a school and right-clicking exit say different things.
        Assert.NotEqual(CastMenuSelection.SchoolButtonHelpDialog, CastMenuSelection.ExitButtonHelpDialog);
    }
}
