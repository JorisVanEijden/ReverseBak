namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

public class SpellInfoPanelTests {
    [Fact]
    public void OnlyTheTitleIsCentred() {
        Assert.Equal(SpellInfoPanel.TitleCentreX - 50, SpellInfoPanel.TitleX(100));
        // Body lines are left-aligned at a fixed x, whatever their width.
        Assert.Equal(SpellInfoPanel.BodyX, SpellInfoPanel.BodyX);
    }

    [Fact]
    public void AnEmptyLineIsSkippedAndDoesNotLeaveAGap() {
        Assert.False(SpellInfoPanel.LineAdvances(""));
        Assert.False(SpellInfoPanel.LineAdvances(null));
        Assert.True(SpellInfoPanel.LineAdvances("Duration: 10 minutes"));

        // Two drawn lines are adjacent even if an empty one sat between them.
        Assert.Equal(SpellInfoPanel.FirstBodyY, SpellInfoPanel.BodyY(0));
        Assert.Equal(SpellInfoPanel.FirstBodyY + SpellInfoPanel.BodyLineStep, SpellInfoPanel.BodyY(1));
    }

    [Fact]
    public void TheCostLineKeepsItsTemplateUntilAPowerIsChosen() {
        Assert.False(SpellInfoPanel.CostLineIsReplaced(0));
        Assert.True(SpellInfoPanel.CostLineIsReplaced(5));
    }

    [Fact]
    public void ADamageOfAThousandMeansNoDamageFigure() {
        // Zero and 1000 both keep the template; only 1000 is the surprising one.
        Assert.False(SpellInfoPanel.DamageLineIsReplaced(0));
        Assert.False(SpellInfoPanel.DamageLineIsReplaced(SpellInfoPanel.NoDamageMagnitude));
        Assert.True(SpellInfoPanel.DamageLineIsReplaced(12));
        Assert.True(SpellInfoPanel.DamageLineIsReplaced(999));
    }

    [Fact]
    public void NineSpellsShowTheCastersHealthAndStamina() {
        Assert.Equal(9, SpellInfoPanel.ShowsCasterHealthStamina.Length);
        Assert.True(SpellInfoPanel.ShowsHealthStamina(0));
        Assert.True(SpellInfoPanel.ShowsHealthStamina(35));
        Assert.False(SpellInfoPanel.ShowsHealthStamina(1));
        Assert.False(SpellInfoPanel.ShowsHealthStamina(36));
    }
}
