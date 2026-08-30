namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Melee, cast or shoot — <c>combataiact_pick_melee_or_missl</c> (CBTAIACT.C:23).
/// </summary>
/// <remarks>
/// <b>The last of the AI-routine duplicates.</b> MonsterActionChoice and
/// MonsterTurnRoutines.CloseOrRanged both modelled this, with the same <c>roll &gt;= distance</c>
/// test; only the latter is dispatched. Unlike the other three this needed a MERGE rather than a
/// deletion — the duplicate carried the quarrel type, the melee delay range and a derived
/// chance-in-ten helper that the survivor lacked. All three moved across first.
/// </remarks>
public class MonsterActionChoiceTests {
    [Fact]
    public void AdjacentMeansMelee_WithNoRollAtAll() {
        Assert.Equal(MonsterMove.Melee,
            MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 1, castRoll: 0).Move);
        Assert.Equal(MonsterMove.Melee,
            MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 1, castRoll: 9).Move);
    }

    [Fact]
    public void SpellsAreTheCLOSERangeOption() {
        // roll >= distance, so the CLOSER the target the likelier a spell. Inverting it has
        // monsters sniping spells across the arena and meleeing nothing.
        Assert.Equal(MonsterMove.Cast,
            MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 2, castRoll: 2).Move);
        Assert.Equal(MonsterMove.Shoot,
            MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 9, castRoll: 2).Move);
    }

    [Fact]
    public void AtTenTilesOrMoreItCanNEVERCast() {
        // A d10 roll cannot reach 10, so the comparison can never hold — an absolute range limit
        // that falls out of the die rather than being tested for.
        for (var roll = 0; roll < MonsterTurnRoutines.CloseRangeCastRollBound; roll++) {
            Assert.Equal(MonsterMove.Shoot,
                MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 10, castRoll: roll).Move);
        }
        Assert.Equal(0, MonsterTurnRoutines.CastChanceInTen(10));
    }

    [Fact]
    public void TheChanceInTenMatchesTheComparisonItIsDerivedFrom() {
        // Eight in ten at two tiles, one in ten at nine — the numbers are the argument for which
        // way round the comparison goes.
        Assert.Equal(8, MonsterTurnRoutines.CastChanceInTen(2));
        Assert.Equal(1, MonsterTurnRoutines.CastChanceInTen(9));
        Assert.Equal(0, MonsterTurnRoutines.CastChanceInTen(1));   // inside melee reach
    }

    [Fact]
    public void TheSpellItCastsIsTheDefaultKind() {
        Assert.Equal(MonsterTurnRoutines.DefaultSpellKind,
            MonsterTurnRoutines.CloseOrRanged(distanceToNearest: 2, castRoll: 9).SpellKind);
    }

    [Fact]
    public void TheMeleeDelayIsARANGE_SoTwoSwingsDoNotLandTogether() {
        Assert.Equal((0x19, 0x31), MonsterTurnRoutines.CloseOrRangedMeleeDelay);
        Assert.True(MonsterTurnRoutines.CloseOrRangedMeleeDelay.Min
            < MonsterTurnRoutines.CloseOrRangedMeleeDelay.Max);
    }
}
