namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Crossing crystal ground sounds cue 69 — TASK-144's last blocked cue that did not need a
/// whole unported feature.
/// </summary>
public class CrystalGroundCueTests {
    [Fact]
    public void TheCueIsZappedAndNotTheFiredCrystalOne() {
        // Two different events on the same puzzle: SHOVING a diamond into a crystal fires it
        // (0x1d), and WALKING on the ground the crystal left behind zaps you (0x45). Reusing one
        // for the other would make the puzzle sound the same whether you solved it or blundered.
        Assert.Equal(0x45, CombatWalk.CrystalGroundSoundId);
        Assert.NotEqual(TrapPuzzle.FiredSoundId, CombatWalk.CrystalGroundSoundId);
    }

    [Fact]
    public void CrystalGroundIsTheHUNDREDDamageOne() {
        // The cue rides on the hazard that deals 100, not on the burning terrain that deals 10-19.
        // Those are different kinds and only one of them is a crystal.
        Assert.Equal(100, CombatWalk.CrystalDamage);
        Assert.NotEqual(TerrainDamage.BurningKind, (int)CombatTerrain.Crystal);
    }
}
