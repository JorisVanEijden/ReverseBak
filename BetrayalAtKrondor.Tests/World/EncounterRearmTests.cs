namespace BetrayalAtKrondor.Tests.World;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Putting a defeated encounter's monsters back on their feet (TASK-212).
/// </summary>
/// <remarks>
/// Read from <c>combatenc_rearm_roster_actors</c> @0x64977 in full. The task's own description
/// asked for "a way to re-read the chapter's enemy-party record"; the original does no such thing —
/// it heals what is already in the save. These pin the difference.
/// </remarks>
public class EncounterRearmTests {
    private static ActorStat[] StatsWithHealth(byte current, byte max) {
        var stats = new ActorStat[16];
        for (int i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat();
        }
        stats[(int)ActorAttribute.Health] = new ActorStat {
            Max = max, Base = current, Effective = current,
        };
        return stats;
    }

    private static SaveGameCombatData Record(byte status) =>
        new SaveGameCombatData(
            targetActorPointer: 0x4321, creatureType: 18,
            xOnGrid: 6, yOnGrid: 7, targetXOnGrid: 2, targetYOnGrid: 3,
            combatStatus: status, animEffectType: 9, activeSpellEffectSlot: 4,
            unusedPadding: 0, animDurationTimer: 5, monsterSpellAbility: 3,
            meleeAttackType: 4, rangedAttackType: 5, movementAiType: 6,
            preferredArrowType: -1, lastSpellSymbolFile: 2,
            floatingDamageValue: 7, floatingDamageTimer: -1);

    [Fact]
    public void AWoundedActorIsHealedToFULL() {
        ActorStat[] stats = StatsWithHealth(current: 9, max: 27);

        Assert.True(EncounterRearm.HealToFull(stats));

        Assert.Equal(27, stats[(int)ActorAttribute.Health].Base);
        Assert.Equal(27, stats[(int)ActorAttribute.Health].Effective);
    }

    [Fact]
    public void AnUntouchedActorReportsNoChange() {
        // So a caller can skip staging an edit that would write the same bytes back.
        ActorStat[] stats = StatsWithHealth(current: 27, max: 27);

        Assert.False(EncounterRearm.HealToFull(stats));
        Assert.False(EncounterRearm.HealToFull(null));
    }

    [Fact]
    public void ONLYHealthIsTouched() {
        // *** The original copies one byte over one byte. *** Stamina, conditions and the rest of
        // the 95-byte record are left exactly as the fight left them; "restore the actor" wholesale
        // hands back a creature the game never intended to reset.
        ActorStat[] stats = StatsWithHealth(current: 9, max: 27);
        stats[(int)ActorAttribute.Stamina] = new ActorStat { Max = 40, Base = 3, Effective = 3 };

        EncounterRearm.HealToFull(stats);

        Assert.Equal(3, stats[(int)ActorAttribute.Stamina].Base);
    }

    [Fact]
    public void TheCombatStatusGoesToOneAndNothingElseMoves() {
        SaveGameCombatData rearmed = EncounterRearm.WithStatusReset(Record(status: 2));

        Assert.Equal(1, rearmed.CombatStatus);
        // Everything else is carried over: the original rewrites the status byte alone.
        Assert.Equal(18, rearmed.CreatureType);
        Assert.Equal(6, rearmed.XOnGrid);
        Assert.Equal(7, rearmed.YOnGrid);
        Assert.Equal(2, rearmed.TargetXOnGrid);
        Assert.Equal(9, rearmed.AnimEffectType);
        Assert.Equal(4, rearmed.ActiveSpellEffectSlot);
        Assert.Equal(0x4321, rearmed.TargetActorPointer);
    }

    [Fact]
    public void TheStatusMatchesTheONEUsedBySaveMigration() {
        // Both write 1 for the same reason — whatever the actor was doing when its fight ended is
        // not something the next fight should inherit. Stated as the same constant so the two
        // cannot drift.
        Assert.Equal(GameData.Resources.Combat.CombatStatePersistence.MigratedCombatStatus,
            EncounterRearm.RearmedCombatStatus);
    }

    [Fact]
    public void ANullRecordIsNotInvented() {
        Assert.Null(EncounterRearm.WithStatusReset(null));
    }

    [Fact]
    public void ElevenEncountersReArmAndTheRestDoNot() {
        Assert.Equal(11, EncounterCompletion.ReArmingEncounters.Count);
        Assert.True(EncounterCompletion.ReArmsWhenDefeated(0xeb));
        Assert.True(EncounterCompletion.ReArmsWhenDefeated(0x1ae));
        Assert.False(EncounterCompletion.ReArmsWhenDefeated(5));
    }
}
