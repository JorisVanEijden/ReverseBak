namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// <c>CombatAffinityTables.AffinityOf</c> — the row a damage application scales by.
/// </summary>
/// <remarks>
/// Indexed by the CREATURE class, which for a party member is their save record's
/// <c>CreatureType</c> and not their roster position. Measured in the original 2026-09-12
/// (TASK-446): Locklear is class 17, Gorath 15 and Owyn 16, and class 16 resists 0x200 — the mask a
/// spell's damage carries — so a reflected Flamecast that should have done 7 to Owyn did 15 while
/// the table was being asked about class 2.
/// </remarks>
public class CombatAffinityLookupTests {
    private static CombatAffinityTables TableWith(params (int ClassId, int Weak, int Resist)[] rows) {
        var t = new CombatAffinityTables("A");
        for (var i = 0; i < 64; i++) {
            t.Creatures.Add(new CreatureAffinity { ClassId = i });
        }
        foreach ((int classId, int weak, int resist) in rows) {
            t.Creatures[classId].WeaknessFlags = weak;
            t.Creatures[classId].ResistanceFlags = resist;
        }

        return t;
    }

    [Fact]
    public void TheRowIsFoundByCreatureClass() {
        CombatAffinityTables t = TableWith((16, 0, 0x0200));

        Assert.Equal(0x0200, t.AffinityOf(16).ResistanceFlags);
        Assert.Equal(0, t.AffinityOf(2).ResistanceFlags);
    }

    [Fact]
    public void OutsideTheTableThereIsNoRowRatherThanAThrow() {
        CombatAffinityTables t = TableWith();

        Assert.Null(t.AffinityOf(-1));
        Assert.Null(t.AffinityOf(64));
    }

    /// <summary>The halving the original applies, through the formula that already models it.</summary>
    [Fact]
    public void AResistedSpellHitIsHalved() {
        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            15, stamina: 16, health: 40,
            immune: false, applyArmor: false, armorRating: 0,
            absorbPool: null, fromDirectAttack: false, negated: false,
            weakToDamageType: false, resistsDamageType: true, _ => 0);

        // 15 >> 1 = 7 — the figure the original threw back at Owyn.
        Assert.Equal(7, outcome.DamageDealt);
    }
}
