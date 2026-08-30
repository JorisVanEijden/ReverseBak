namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The spit/breathe/hurl turn — <c>combataiact_ranged_attack_turn</c> (CBTAIACT.C:85).
/// </summary>
/// <remarks>
/// <b>Written against a second model of this routine.</b> MonsterRangedTurn and
/// MonsterTurnRoutines.ChooseRangedTurn both described it; only the latter is dispatched
/// (MonsterTurnResolver maps RangedAttackTurn to it). The survivor carried everything the
/// duplicate did — the same four creature entries with the same action ids and knockbacks, the
/// same damage bands, the same AlwaysHeavyCreature — plus the light shot and the shared RangedTurn
/// vocabulary, so this was a plain deletion.
/// </remarks>
public class MonsterRangedTurnTests {
    [Fact]
    public void HalfTheTimeItDoesNotShootEvenWithAClearLine() {
        // The abort roll is checked BEFORE anything else and hands the turn to the generic
        // move/attack picker, so these creatures spend about half their turns repositioning.
        // Skipping it roughly doubles their damage output.
        //
        // *** A LOW ROLL ABORTS. *** The test is `abortRoll < AbortShotRoll`, so the shot happens
        // on the HIGH half — the opposite of the reflex, and the way I first wrote this test.
        Assert.Equal(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(lineOfFireClear: true,
                abortRoll: MonsterTurnRoutines.AbortShotRoll - 1,
                heavyRoll: 0, creatureType: 0x29).Choice);
        Assert.NotEqual(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(lineOfFireClear: true,
                abortRoll: MonsterTurnRoutines.AbortShotRoll,
                heavyRoll: 0, creatureType: 0x29).Choice);
    }

    [Fact]
    public void NoLineOfFireMeansNoShot() {
        Assert.Equal(MonsterTurnRoutines.RangedChoice.Reconsider,
            MonsterTurnRoutines.ChooseRangedTurn(lineOfFireClear: false,
                abortRoll: MonsterTurnRoutines.AbortShotRoll,
                heavyRoll: 0, creatureType: 0x29).Choice);
    }

    [Fact]
    public void TheHeavyShotIsTheCOMMONCase() {
        // Three rolls in four take it, and its damage band is well over double the light one's —
        // so which branch a port picks by default matters a lot.
        MonsterTurnRoutines.RangedTurn heavy = MonsterTurnRoutines.ChooseRangedTurn(
            lineOfFireClear: true, abortRoll: MonsterTurnRoutines.AbortShotRoll,
            heavyRoll: 0, creatureType: 0x29);
        MonsterTurnRoutines.RangedTurn light = MonsterTurnRoutines.LightShot();

        Assert.Equal(MonsterTurnRoutines.RangedChoice.HeavyShot, heavy.Choice);
        Assert.True(heavy.MinDamage > light.MaxDamage * 2);
    }

    [Fact]
    public void EachHeavyCreatureHasItsOwnActionAndKnockback() {
        // Four types, and the action id and knockback differ per type — a single shared pair would
        // give three of them the wrong animation.
        Assert.Equal((2, 1), Pair(0x29));
        Assert.Equal((3, 3), Pair(0x2a));
        Assert.Equal((0x32, 3), Pair(0x2b));
        Assert.Equal((0x32, 3), Pair(MonsterTurnRoutines.AlwaysHeavyCreature));

        static (int, int) Pair(int creature) {
            MonsterTurnRoutines.RangedTurn t = MonsterTurnRoutines.HeavyShotFor(creature).Value;
            return (t.ActionId, t.KnockbackFrames);
        }
    }

    [Fact]
    public void AnUnlistedCreatureIsREFUSED_NotGivenAGuess() {
        // The original's switch has no default: an unlisted creature reaching the heavy branch
        // leaves actionId and knockback UNINITIALISED and attacks with whatever was on the stack.
        // That is a latent bug, unreachable only while the discipline stays on these four types,
        // and refusing is the honest port of undefined behaviour.
        Assert.Null(MonsterTurnRoutines.HeavyShotFor(0x01));
    }
}
