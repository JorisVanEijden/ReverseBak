namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Spell fields painted on the arena floor, and what they do to whoever stands in one.
/// </summary>
public class TileEffectTests {
    [Fact]
    public void AuthoredTerrainCarriesNOTimerAndTheSweepCannotTouchIt() {
        // *** THE SAFETY THAT MAKES A WHOLE-GRID SWEEP SAFE. *** Timers start at -1, and the tick
        // touches a cell only when its timer is >= 0. Defaulting them to 0 instead would expire
        // every crystal, cannon and wall on the arena one tick into the first fight.
        var grid = new CombatGrid();
        grid.SetTerrain(3, 4, CombatTerrain.Crystal);
        grid.SetTerrain(5, 6, CombatTerrain.CannonEast);

        Assert.Equal(CombatGrid.NoEffect, grid.EffectTimerAt(3, 4));
        Assert.Equal(0, grid.TickTileEffects());
        Assert.Equal(CombatTerrain.Crystal, grid.TerrainAt(3, 4));
        Assert.Equal(CombatTerrain.CannonEast, grid.TerrainAt(5, 6));
    }

    [Fact]
    public void APaintedFieldOVERWRITESTheTerrainWhileItBurns() {
        // The effect IS the terrain — there is no overlay and no second field to consult, which is
        // why standing in one denies shooting and casting.
        var grid = new CombatGrid();
        grid.SetTerrain(2, 2, CombatTerrain.Crystal);

        grid.SetTileEffect(2, 2, (CombatTerrain)CombatCapability.DenyingTerrain, 2);

        Assert.Equal(CombatCapability.DenyingTerrain, (int)grid.TerrainAt(2, 2));
    }

    [Fact]
    public void ZeroIsTheLASTTick_notAnExpiredOne() {
        // The original tests == 0 and expires there, so a field painted with N burns for N+1
        // ticks. Decrementing first and testing after costs every field one tick.
        var grid = new CombatGrid();
        grid.SetTileEffect(1, 1, (CombatTerrain)CombatCapability.DenyingTerrain, 2);

        Assert.False(grid.TickTileEffect(1, 1));   // 2 -> 1
        Assert.False(grid.TickTileEffect(1, 1));   // 1 -> 0
        Assert.True(grid.TickTileEffect(1, 1), "the third tick is the one that expires it");
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(1, 1));
        Assert.Equal(CombatGrid.NoEffect, grid.EffectTimerAt(1, 1));
    }

    [Fact]
    public void KindNINEExpiresToCRYSTAL_everythingElseToOpen() {
        // *** THE ONE EXCEPTION IN THE EXPIRY BRANCH. *** Kind 9 is what a rising Black Slayer
        // paints (SlayerRevival.RisenTileEffect), and it leaves crystal ground behind rather than
        // bare floor. Reading the branch as "clear the tile" loses that outright.
        var grid = new CombatGrid();
        grid.SetTileEffect(4, 4, (CombatTerrain)CombatGrid.RevertsToCrystalKind, 0);
        grid.SetTileEffect(5, 4, (CombatTerrain)CombatCapability.DenyingTerrain, 0);

        Assert.Equal(2, grid.TickTileEffects());
        Assert.Equal(CombatTerrain.Crystal, grid.TerrainAt(4, 4));
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(5, 4));
    }

    [Fact]
    public void AnExpiredCellIsIndistinguishableFromOneThatNeverBurned() {
        // Which is what stops it being expired twice.
        var grid = new CombatGrid();
        grid.SetTileEffect(0, 5, (CombatTerrain)CombatCapability.DenyingTerrain, 0);

        Assert.True(grid.TickTileEffect(0, 5));
        Assert.False(grid.TickTileEffect(0, 5), "a lapsed cell is not lapsed again");
    }

    [Fact]
    public void ONLYKindOneBurns() {
        // *** THE SWITCH HAS EXACTLY ONE CASE. *** Everything else a cell can be — crystal,
        // cannon, trap, the pushable diamond — is walked over without damage. Treating "there is
        // an effect here" as "it hurts" invents harm the game does not deal.
        Assert.True(TerrainDamage.Burns(TerrainDamage.BurningKind, occupied: true,
            occupantResists: false));

        foreach (int kind in new[] { 0, 2, 3, 5, 6, 7, 8, 9, 10, 11 }) {
            Assert.False(TerrainDamage.Burns(kind, occupied: true, occupantResists: false),
                $"kind {kind} must not burn");
        }
    }

    [Fact]
    public void AnEmptyBurningTileDoesNothing_andResistanceSpares() {
        Assert.False(TerrainDamage.Burns(TerrainDamage.BurningKind, occupied: false,
            occupantResists: false));
        Assert.False(TerrainDamage.Burns(TerrainDamage.BurningKind, occupied: true,
            occupantResists: true));
    }

    [Fact]
    public void TheDamageBandIsInclusiveAtBothEnds() {
        // RNDR(10, 19) — ten possible values, not nine.
        Assert.Equal(10, TerrainDamage.DamageFor(_ => 0));
        Assert.Equal(19, TerrainDamage.DamageFor(n => n - 1));
    }
}
