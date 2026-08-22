namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The two predicates behind the combat HUD's capability cell. Both are situational, which is the
/// part a port gets wrong.
/// </summary>
public class CombatCapabilityTests {
    private static readonly int[] Thresholds = { 10, 20, 30 };

    private static bool Shoot(int distance = 5, int quarrels = 4, bool weapon = true,
        int creature = 0, int skill = 30, int terrain = 0) =>
        CombatCapability.CanShoot(terrain, distance, quarrels, weapon, creature, skill);

    private static bool Cast(int distance = 5, int skill = 30, int health = 40,
        int terrain = 0, bool chapterEightBlocked = false) =>
        CombatCapability.CanCast(terrain, distance, skill, health, Thresholds, chapterEightBlocked);

    [Fact]
    public void AnArmedArcherWithRoomCanShoot() {
        Assert.True(Shoot());
    }

    [Fact]
    public void AnAdjacentEnemyTakesAwayBothOptions() {
        // *** The rule a port drops. *** These are per-turn decisions: an enemy closing to melee
        // removes the button, so computing capability once at the start of a fight is wrong from the
        // second turn on.
        Assert.False(Shoot(distance: 1), "someone is adjacent");
        Assert.False(Cast(distance: 1), "same rule, written as >= 2 in the original");
        Assert.True(Shoot(distance: 2), "two tiles is already clear");
        Assert.True(Cast(distance: 2));
    }

    [Fact]
    public void AmmunitionAndAWeaponAreBothRequired() {
        Assert.False(Shoot(quarrels: 0), "a bow with nothing to fire");
        Assert.False(Shoot(weapon: false), "bolts but nothing to fire them from");
    }

    [Fact]
    public void TheInnatelyMissileCreatureNeedsNeither() {
        // The carried-gear test is an OR against creature type, so this one shoots empty-handed.
        Assert.True(Shoot(quarrels: 0, weapon: false,
            creature: CombatCapability.InnatelyMissileCreature));
    }

    [Fact]
    public void ZeroSkillRefusesRatherThanMissing() {
        Assert.False(Shoot(skill: 0));
        Assert.False(Cast(skill: 0));
    }

    [Fact]
    public void TheDenyingTerrainBlocksBoth() {
        Assert.False(Shoot(terrain: CombatCapability.DenyingTerrain));
        Assert.False(Cast(terrain: CombatCapability.DenyingTerrain));
        Assert.Equal(1, CombatCapability.DenyingTerrain);
    }

    [Fact]
    public void CastingIsGatedOnHealth_NotOnKnowingSpells() {
        // *** The finding that names this wrongly. *** The original's nine
        // combatenc_actor_stat_above_table calls all compare stat 0 - HEALTH - against different
        // entries of one table, and only "not zero" is used. So it reduces to clearing the LOWEST
        // threshold. Nothing here consults a spell list at all.
        Assert.True(Cast(health: 11), "just above the lowest threshold of 10");
        Assert.False(Cast(health: 10), "equal is not above");
        Assert.False(Cast(health: 0), "a downed caster");
    }

    [Fact]
    public void TheUncheckedDistanceSkipsTheAdjacencyRule() {
        // find_nearest = 0 substitutes 100, which is how a caller asks "can they cast at all?"
        // without caring who is standing next to them.
        Assert.True(Cast(distance: CombatCapability.DistanceUnchecked));
        Assert.Equal(100, CombatCapability.DistanceUnchecked);
    }

    [Fact]
    public void ChapterEightWithoutTheRequiredItemRefusesCasting() {
        Assert.False(Cast(chapterEightBlocked: true), "and nothing else can rescue it");
        Assert.False(Cast(chapterEightBlocked: true, health: 99, skill: 99));
    }

    [Fact]
    public void AnEmptyThresholdLadderClearsNothing() {
        // Defensive: a missing table must not silently permit casting at any health.
        Assert.False(CombatCapability.ClearsAnyThreshold(999, null));
        Assert.False(CombatCapability.ClearsAnyThreshold(999, new int[0]));
    }
}
