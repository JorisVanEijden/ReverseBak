namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The cast screen's two call contexts: which REQ layout loads, and where the opening school comes
/// from in each.
/// </summary>
public class CastScreenContextTests {
    [Fact]
    public void TheLayoutIsChosenByTheCasterNotByAModeFlag() {
        Assert.Equal(CastMenuSelection.CombatLayout,
            CastMenuSelection.LayoutFor(casterHasCombatData: true));
        Assert.Equal(CastMenuSelection.FieldLayout,
            CastMenuSelection.LayoutFor(casterHasCombatData: false));
    }

    [Fact]
    public void ACombatCastReadsTheSchoolOffTheCombatant() {
        // Not from the sticky overworld pair, which is why a combat cast cannot leak into it.
        Assert.Equal(2, CastMenuSelection.OpeningSchool(casterHasCombatData: true,
            combatantSchool: 2, rememberedSchool: 4));
    }

    [Fact]
    public void AFieldCastUsesTheRememberedSchool() {
        Assert.Equal(4, CastMenuSelection.OpeningSchool(casterHasCombatData: false,
            combatantSchool: 2, rememberedSchool: 4));
    }

    [Fact]
    public void AndFallsBackToTheLastSchoolWhenNothingIsRemembered() {
        Assert.Equal(CastMenuSelection.DefaultSchool,
            CastMenuSelection.OpeningSchool(casterHasCombatData: false, combatantSchool: 2,
                rememberedSchool: CastMenuSelection.None));
    }

    [Fact]
    public void SwitchingSchoolsIsALoadNotASwap() {
        Assert.True(CastMenuSelection.SchoolSwitchReloadsSymbolData);
    }
}
