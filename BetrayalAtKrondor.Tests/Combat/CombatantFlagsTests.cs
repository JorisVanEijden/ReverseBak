namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The combatant status bits, against <c>INCLUDE/defines.h</c>.
/// </summary>
public class CombatantFlagsTests {
    [Fact]
    public void TheBitsAreTheOnesTheHeaderDefines() {
        // CAF_READY 0x01, CAF_DEAD 0x02, CAF_DEFEND_CMD 0x04, CAF_PARRY 0x08,
        // CAF_FLEE 0x10, CAF_POISON 0x20, CAF_KNOCKBACK 0x40, CAF_AI_SUMMON 0x80.
        //
        // Pinned one by one because two of them were wrong: Fleeing was 0x20 (which is POISON) and
        // 0x10 held a speculative "Defending". Nothing caught it, because no test compared the enum
        // to the header — only to itself.
        Assert.Equal(0x01, (int)CombatantFlags.Ready);
        Assert.Equal(0x02, (int)CombatantFlags.Dead);
        Assert.Equal(0x04, (int)CombatantFlags.DefendCommand);
        Assert.Equal(0x08, (int)CombatantFlags.Parry);
        Assert.Equal(0x10, (int)CombatantFlags.Fleeing);
        Assert.Equal(0x20, (int)CombatantFlags.Poisoned);
        Assert.Equal(0x40, (int)CombatantFlags.Knockback);
        Assert.Equal(0x80, (int)CombatantFlags.AiSummon);
    }

    [Fact]
    public void EveryBitIsDistinct() {
        var values = new[] {
            CombatantFlags.Ready, CombatantFlags.Dead, CombatantFlags.DefendCommand,
            CombatantFlags.Parry, CombatantFlags.Fleeing, CombatantFlags.Poisoned,
            CombatantFlags.Knockback, CombatantFlags.AiSummon,
        };
        var seen = 0;
        foreach (CombatantFlags f in values) {
            Assert.Equal(0, seen & (int)f);
            seen |= (int)f;
        }
        Assert.Equal(0xFF, seen);
    }

    [Fact]
    public void TheRoundResetLeavesARoutingMonsterRouting() {
        // *** What the wrong bit actually cost. *** combatenc_refresh_actor_flags clears
        // CAF_DEFEND_CMD and nothing else. While Fleeing was numbered 0x10 and BeginRound cleared
        // the flag called "Defending" — also 0x10 — a monster that decided to run had the decision
        // wiped at the next round boundary and went back to fighting.
        var encounter = new CombatEncounter();
        var monster = new Combatant {
            PartySlot = 0,
            Health = 4,
            Speed = 3,
            Flags = CombatantFlags.Fleeing | CombatantFlags.DefendCommand,
        };
        encounter.Enemies.Add(monster);
        encounter.Party.Add(new Combatant { PartySlot = 1, Health = 10, Speed = 3 });

        encounter.BeginRound();

        Assert.Equal(CombatantFlags.Fleeing, monster.Flags & CombatantFlags.Fleeing);
        Assert.Equal(CombatantFlags.None, monster.Flags & CombatantFlags.DefendCommand);
        Assert.Equal(CombatantFlags.Ready, monster.Flags & CombatantFlags.Ready);
    }
}
