namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Whether a creature routs — <c>combatenc_ai_morale_flee_check</c>.
/// </summary>
public class MonsterMoraleTests {
    // g_ai_flee_threshold_table, extracted to generated/EXE/combat-affinity.json.
    private static readonly int[] Thresholds = { 85, 55, 45, 35, 25, 20, 10, 5, 5, 0 };

    private static bool Routs(int staminaPercent, int morale, int roll, bool underground = false) =>
        MonsterMorale.Routs(staminaPercent, morale, roll, Thresholds, underground);

    [Fact]
    public void MoraleFFNeverRoutsAtAll() {
        // Rejected before anything is computed — the fearless flag.
        Assert.False(Routs(staminaPercent: 0, morale: MonsterMorale.NeverFleesMorale, roll: 0));
    }

    [Fact]
    public void NothingRoutsUnderground() {
        // g_game_mode == 2 returns immediately, so a dungeon fight is always to the finish. A roll
        // that would rout anywhere else does nothing here.
        Assert.True(Routs(staminaPercent: 10, morale: 8, roll: 0));
        Assert.False(Routs(staminaPercent: 10, morale: 8, roll: 0, underground: true));
    }

    [Fact]
    public void MoraleZeroSpendsTheRollAndThenRefusesToFlee() {
        // *** The ordering that matters. *** Morale 0 is rejected AFTER the roll, not before, so it
        // never flees but does consume the roll. Folding it into the early guard would keep the
        // outcome and desynchronise a shared RNG.
        Assert.False(Routs(staminaPercent: 10, morale: 0, roll: 0));
        Assert.True(MonsterMorale.ConsumesARoll(morale: 0, isUnderground: false));
        Assert.False(MonsterMorale.ConsumesARoll(MonsterMorale.NeverFleesMorale, isUnderground: false));
        Assert.False(MonsterMorale.ConsumesARoll(morale: 4, isUnderground: true));
    }

    [Fact]
    public void TheTableEntryIsThePercentChanceToRout() {
        // Flees when the roll comes in strictly under the threshold. Index 0 is 85, so a roll of 84
        // routs and 85 does not.
        Assert.Equal(0, MonsterMorale.IndexFor(staminaPercent: 10, morale: MonsterMorale.MoralePivot));
        Assert.True(Routs(staminaPercent: 10, morale: 8, roll: 84));
        Assert.False(Routs(staminaPercent: 10, morale: 8, roll: 85));
    }

    [Fact]
    public void AFullyRestedCreatureAtTheBestIndexNeverRouts() {
        // Index 9 holds 0, and nothing is strictly under 0.
        Assert.Equal(9, MonsterMorale.IndexFor(staminaPercent: 100, morale: MonsterMorale.MoralePivot));
        Assert.False(Routs(staminaPercent: 100, morale: 8, roll: 0));
    }

    [Fact]
    public void AHigherStatValueMakesACreatureMoreLikelyToRout_NotLess() {
        // *** The polarity trap. *** The term is 8 - value, and a LARGER index is the CALM end of
        // the table — so 8 is the jumpiest a creature can be and 0 the steadiest. Both names for
        // this field (canassa's "morale", our "FleeThreshold") read the other way round in English.
        Assert.True(MonsterMorale.HigherValueMeansMoreLikelyToRout);
        Assert.True(MonsterMorale.IndexFor(50, morale: 2) > MonsterMorale.IndexFor(50, morale: 8));
        Assert.True(Routs(staminaPercent: 50, morale: 8, roll: 20), "the 8 runs");
        Assert.False(Routs(staminaPercent: 50, morale: 2, roll: 20), "the 2 holds its ground");
    }

    [Fact]
    public void TheDescentIsSteepAtTheStartAndFlatAtTheEnd() {
        // 85 -> 55 -> 45 -> 35 -> 25 -> 20 -> 10 -> 5 -> 5 -> 0: most of the behaviour lives in the
        // first few steps, so a wounded creature is nearly certain to run and the tail barely moves.
        Assert.Equal(85, Thresholds[0]);
        Assert.Equal(30, Thresholds[0] - Thresholds[1]);
        Assert.Equal(0, Thresholds[7] - Thresholds[8]);
        Assert.Equal(0, Thresholds[9]);
    }

    [Fact]
    public void TheIndexIsCappedAtTheTopOfTheTable() {
        Assert.Equal(MonsterMorale.MaxIndex, MonsterMorale.IndexFor(staminaPercent: 100, morale: 0));
    }

    [Fact]
    public void ANegativeIndexIsClampedRatherThanReadingOffTheFrontOfTheTable() {
        // The original caps the top and not the bottom: 0% stamina gives -1 before the morale term,
        // so a creature of morale 8 or better indexes at -1 and the original reads out of bounds.
        // We clamp to 0, which is the same answer wherever the original is in bounds and a defined
        // one where it is not — and 0 is the rout-most end, which is where such a creature belongs.
        Assert.Equal(0, MonsterMorale.IndexFor(staminaPercent: 0, morale: 8));
        Assert.Equal(0, MonsterMorale.IndexFor(staminaPercent: 0, morale: 20));
        Assert.True(Routs(staminaPercent: 0, morale: 8, roll: 84));
    }

    [Fact]
    public void AMissingTableIsRefusedRatherThanThrowing() {
        Assert.False(MonsterMorale.Routs(10, 8, 0, null, isUnderground: false));
        Assert.False(MonsterMorale.Routs(10, 8, 0, new int[0], isUnderground: false));
    }
}
