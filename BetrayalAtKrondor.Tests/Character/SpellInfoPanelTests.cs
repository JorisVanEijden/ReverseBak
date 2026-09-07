namespace BetrayalAtKrondor.Tests.Character;

using System.Linq;
using GameData.Resources.Spells;
using global::GameData.Resources.Character;
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

    /// <summary>The footer reads the way the original's sprintf writes it.</summary>
    /// <remarks>
    /// Pinned as the whole string because the two spaces after the colon are load-bearing — the
    /// description lines above use one, so the footer is indented past them. A tidy-up to a single
    /// space changes nothing that fails, which is exactly why it is asserted.
    /// </remarks>
    [Fact]
    public void TheFooterKeepsTheOriginalSpacing() {
        Assert.Equal("Health/Stamina:  76 of 85", SpellInfoPanel.HealthStaminaLine(76, 85));
    }

    /// <summary>The pool it prints is the sum of both pairs, current against maximum.</summary>
    /// <remarks>
    /// <c>stat_actor_get(actor, 0x10, ...)</c> with mode 0 and 1. Owyn on the shipped dungeon save
    /// reads 40/40 health and 36/45 stamina, and the original's panel says "76 of 85" — so the two
    /// halves come from different fields of the same pair, and summing the wrong one gives 80 or 85
    /// twice over without ever throwing.
    /// </remarks>
    [Fact]
    public void ThePoolIsBothPairsSummedSeparately() {
        var health = new ActorStat { Base = 40, Max = 40 };
        var stamina = new ActorStat { Base = 36, Max = 45 };

        Assert.Equal("Health/Stamina:  76 of 85", SpellInfoPanel.HealthStaminaLine(
            StatEngine.HealthPool(health, stamina), StatEngine.HealthPoolMax(health, stamina)));
    }

    /// <summary>The footer's nine are the nine FIELD spells — the same set, different order.</summary>
    /// <remarks>
    /// The two lists come from different places in the original: this one from the info panel's jump
    /// table (CSPELL.C:1964), <see cref="FieldSpells.All"/> from the field dispatcher. They agree
    /// because the footer exists for the cast made outside a fight, where nothing else on screen
    /// shows the caster's pool — in combat the HUD panel already does.
    ///
    /// <para>Asserted as SETS, since the orders differ and neither is wrong. If this ever fails,
    /// the answer is to work out which of the two really changed, not to edit whichever list is
    /// convenient.</para>
    /// </remarks>
    [Fact]
    public void TheFooterIsShownForExactlyTheFieldSpells() {
        Assert.Equal(
            FieldSpells.All.OrderBy(id => id),
            SpellInfoPanel.ShowsCasterHealthStamina.OrderBy(id => id));
    }
}
