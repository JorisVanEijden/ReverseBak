namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Data;
using Xunit;

/// <summary>Mapping a live combatant onto its saved 22-byte record.</summary>
public class CombatStatePersistenceTests {
    private static SaveGameCombatData Existing() => new SaveGameCombatData(
        targetActorPointer: 0x1234, creatureType: 9,
        xOnGrid: 1, yOnGrid: 2, targetXOnGrid: 3, targetYOnGrid: 4,
        combatStatus: 5, animEffectType: 6, activeSpellEffectSlot: 7, unusedPadding: 8,
        animDurationTimer: 9, monsterSpellAbility: 10, meleeAttackType: 11, rangedAttackType: 12,
        movementAiType: 13, preferredArrowType: -1, lastSpellSymbolFile: 15,
        floatingDamageValue: 16, floatingDamageTimer: -1);

    [Fact]
    public void EveryFieldTheFightDoesNotOwnIsCarriedThrough() {
        // *** The whole design. *** Nineteen fields, four of which a fight knows about; the rest come
        // from the creature's own data. Building a record instead of patching one would zero the
        // attack types, the movement AI kind and the spell ability — meaningless values the engine
        // then reads as real ones.
        SaveGameCombatData before = Existing();

        SaveGameCombatData after = CombatStatePersistence.WithLiveState(
            before, new Combatant { X = 6, Y = 7 });

        Assert.Equal(before.CreatureType, after.CreatureType);
        Assert.Equal(before.MonsterSpellAbility, after.MonsterSpellAbility);
        Assert.Equal(before.MeleeAttackType, after.MeleeAttackType);
        Assert.Equal(before.RangedAttackType, after.RangedAttackType);
        Assert.Equal(before.MovementAiType, after.MovementAiType);
        Assert.Equal(before.PreferredArrowType, after.PreferredArrowType);
        Assert.Equal(before.AnimDurationTimer, after.AnimDurationTimer);
        Assert.Equal(before.ActiveSpellEffectSlot, after.ActiveSpellEffectSlot);
    }

    [Fact]
    public void ThePositionIsTakenFromTheLiveCombatant() {
        SaveGameCombatData after = CombatStatePersistence.WithLiveState(
            Existing(), new Combatant { X = 6, Y = 7 });

        Assert.Equal(6, after.XOnGrid);
        Assert.Equal(7, after.YOnGrid);
    }

    [Fact]
    public void TheStoredTargetPointerIsZeroedRatherThanReproduced() {
        // It is a heap address from the saving session and means nothing in a later run — the
        // original's own save/load already carries a dangling value across. Zero is what the
        // engine's code tests for, so it reads as "no target" rather than as a wild pointer.
        SaveGameCombatData after = CombatStatePersistence.WithLiveState(
            Existing(), new Combatant { X = 1, Y = 1 });

        Assert.Equal(CombatStatePersistence.NoTargetPointer, after.TargetActorPointer);
        Assert.NotEqual(Existing().TargetActorPointer, after.TargetActorPointer);
    }

    [Fact]
    public void ATargetsTILE_IsRecordedEvenThoughItsPointerIsNot() {
        // The tile is real data the fight owns; only the pointer is unreproducible. Dropping both
        // would lose where the creature was aiming.
        var target = new Combatant { X = 5, Y = 6 };

        SaveGameCombatData after = CombatStatePersistence.WithLiveState(
            Existing(), new Combatant { X = 1, Y = 1, Target = target });

        Assert.Equal(5, after.TargetXOnGrid);
        Assert.Equal(6, after.TargetYOnGrid);
    }

    [Fact]
    public void WithNoTargetTheRecordedTileIsLeftAsItWas() {
        // Zeroing it would claim the creature is aiming at tile (0,0), a real corner of the grid.
        SaveGameCombatData before = Existing();

        SaveGameCombatData after = CombatStatePersistence.WithLiveState(
            before, new Combatant { X = 1, Y = 1, Target = null });

        Assert.Equal(before.TargetXOnGrid, after.TargetXOnGrid);
        Assert.Equal(before.TargetYOnGrid, after.TargetYOnGrid);
    }
}
