namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// A trap puzzle's win condition, and the fact that the encounter consults it.
/// </summary>
/// <remarks>
/// <b>The rule existed and nothing called it.</b> <see cref="TrapPuzzleGoal.PartyIsOut"/> had no
/// production caller, so <c>HasObjective</c> was a condition with no way to discharge it: a party
/// that crossed zone 1 tile (10,11)'s hazards and stood on the exit tile was told the fight was
/// still on, and the only way out of the game was to restart it.
/// </remarks>
public class TrapPuzzleObjectiveTests {
    private static CombatEncounter WithObjective(int exitRow, params int[] partyRows) {
        var encounter = new CombatEncounter { HasObjective = true, ObjectiveExitRow = exitRow };
        foreach (int row in partyRows) {
            encounter.Party.Add(new Combatant { PartySlot = 1, Y = row, Health = 10 });
        }
        return encounter;
    }

    [Fact]
    public void ReachingTheExitRowEndsThePuzzle() {
        Assert.False(WithObjective(6, 0, 1, 2).IsOver());
        Assert.True(WithObjective(6, 0, 1, 6).IsOver());
    }

    [Fact]
    public void OneMemberIsEnough_AndPassingTheRowCountsToo() {
        // PartyIsOut's own rule: `row >= exitRow`, any single member. The measured crossing put all
        // three on row 6 with the exit at (7,6); nobody has to stand on the marked cell.
        Assert.True(WithObjective(6, 7).IsOver());
    }

    [Fact]
    public void ACorpsePastTheLineDoesNotWinIt() {
        var encounter = new CombatEncounter { HasObjective = true, ObjectiveExitRow = 6 };
        encounter.Party.Add(new Combatant { PartySlot = 1, Y = 6, Flags = CombatantFlags.Dead });
        encounter.Party.Add(new Combatant { PartySlot = 2, Y = 0, Health = 10 });
        Assert.False(encounter.ObjectiveIsMet());

        // …and the grid's hazards drop you exactly there, which is why this is not hypothetical:
        // one crystal step is 100 damage, so the first casualty falls mid-crossing.
        Assert.False(encounter.IsOver());
    }

    [Fact]
    public void WithoutAnObjectiveTheRowMeansNothing() {
        var encounter = new CombatEncounter { HasObjective = false, ObjectiveExitRow = 6 };
        encounter.Party.Add(new Combatant { PartySlot = 1, Y = 9, Health = 10 });
        Assert.False(encounter.ObjectiveIsMet());
        // No enemies and no objective is the ordinary end, and it still works.
        Assert.True(encounter.IsOver());
    }

    [Fact]
    public void TheRowOverloadAgreesWithTheGridOne() {
        var grid = new CombatGrid();
        grid.SetTerrain(7, 6, CombatTerrain.Exit);
        Assert.Equal(6, TrapPuzzleGoal.ExitRow(grid));
        Assert.Equal(TrapPuzzleGoal.PartyIsOut(grid, new[] { 6 }),
            TrapPuzzleGoal.PartyIsOut(6, new[] { 6 }));
        Assert.Equal(TrapPuzzleGoal.PartyIsOut(grid, new[] { 5 }),
            TrapPuzzleGoal.PartyIsOut(6, new[] { 5 }));
    }
}
