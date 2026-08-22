namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Choosing where a summoned creature lands.</summary>
public class SummonPlacementTests {
    private static CombatGrid Grid() => new CombatGrid();

    [Fact]
    public void OffTheGridIsNeitherHighlightedNorAccepted() {
        CombatGrid g = Grid();
        Assert.False(SummonPlacement.Highlights(g, -1, 5));
        Assert.False(SummonPlacement.Highlights(g, 8, 5));
        Assert.False(SummonPlacement.Highlights(g, 3, 13));
        Assert.False(SummonPlacement.Accepts(g, 8, 5));
    }

    [Fact]
    public void AnOrdinaryOpenCellHighlightsAndAccepts() {
        CombatGrid g = Grid();
        Assert.True(SummonPlacement.Highlights(g, 3, 5));
        Assert.True(SummonPlacement.Accepts(g, 3, 5));
        Assert.Equal(SummonPlacement.LegalHighlight, SummonPlacement.HighlightFor(g, 3, 5));
    }

    [Fact]
    public void ABlockedCellDoesNeither() {
        CombatGrid g = Grid();
        g.SetTerrain(3, 5, CombatTerrain.Wall);
        Assert.False(SummonPlacement.Highlights(g, 3, 5));
        Assert.False(SummonPlacement.Accepts(g, 3, 5));
        Assert.Equal(SummonPlacement.NoHighlight, SummonPlacement.HighlightFor(g, 3, 5));
    }

    [Fact]
    public void CRYSTALGROUNDHIGHLIGHTSBUTSWALLOWSTHECLICK() {
        // *** The asymmetry, and it is the original's. *** The acceptance test adds one condition
        // the highlight does not. Crystal ground is deliberately NOT blocking — walking onto it is
        // how it goes off — so it passes the highlight test and then refuses the click, silently.
        // Highlighting from the acceptance test would be tidier and would show a different grid
        // than the game does.
        CombatGrid g = Grid();
        g.SetTerrain(3, 5, CombatTerrain.Crystal);

        Assert.True(SummonPlacement.Highlights(g, 3, 5), "it looks placeable");
        Assert.False(SummonPlacement.Accepts(g, 3, 5), "and is not");
        // The renderer calls HighlightFor, so THAT is where the tidy-up would land — asserting only
        // the two predicates above let a mutant that derives the highlight from Accepts pass.
        Assert.Equal(SummonPlacement.LegalHighlight, SummonPlacement.HighlightFor(g, 3, 5));
    }

    [Fact]
    public void THEPICKERCANNOTBECANCELLED() {
        // The loop ends only on a valid click — no cancel, no escape. Wiring an Esc would add a way
        // out the original does not have.
        Assert.False(SummonPlacement.CanCancel);
    }

    [Fact]
    public void AnOccupiedCellIsBlockedAndSoRefused() {
        CombatGrid g = Grid();
        g.SetOccupied(3, 5, true);
        Assert.False(SummonPlacement.Accepts(g, 3, 5));
    }
}
