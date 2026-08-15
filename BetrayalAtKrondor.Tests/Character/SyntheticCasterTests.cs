namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// castCombatSpell — the path that lets a trap or a script cast a spell with no caster behind it,
/// and the health gate every caster action runs first.
/// </summary>
public class SyntheticCasterTests {
    [Fact]
    public void TheNegatedPowerIsWhatMakesACasterlessCastWork() {
        // A negative cost exempts the cast from the to-hit roll and from billing — exactly what a
        // caster with no skill and no health needs.
        Assert.Equal(-14, SpellCastRoutines.SyntheticCasterPower(14));
        Assert.True(SpellCostModifiers.IsNegated(SpellCastRoutines.SyntheticCasterPower(14)));
        Assert.False(SpellHitResolution.CanMiss(targetingType: 0, costWasNegated: true,
            hasTarget: true));
    }

    [Fact]
    public void AndItStillArrivesAsAPositiveMagnitude() {
        // The dispatcher strips the sign, so the effect scales from the power that was asked for.
        Assert.Equal(14, SpellCostModifiers.Effective(
            SpellCastRoutines.SyntheticCasterPower(14), surcharged: false, targetIsWeak: false));
    }

    [Fact]
    public void StrengthDrainIsTheOneSpellThisPathRefuses() {
        // It transfers to the caster, and there is no caster to receive it.
        Assert.True(SpellCastRoutines.SyntheticCasterRefuses(SpellIds.StrengthDrain));
        Assert.False(SpellCastRoutines.SyntheticCasterRefuses(SpellIds.Flamecast));
        Assert.False(SpellCastRoutines.SyntheticCasterRefuses(SpellIds.Skyfire));
    }

    [Fact]
    public void EveryUsedHealthBracketIsTheSameNumber() {
        // Three call sites pick brackets 0, 1 and 2; the shipped table holds 10 in all three.
        Assert.Equal(10, MonsterSpellcasting.HealthGateThresholds[0]);
        Assert.Equal(10, MonsterSpellcasting.HealthGateThresholds[1]);
        Assert.Equal(10, MonsterSpellcasting.HealthGateThresholds[2]);
    }

    [Fact]
    public void TheGateIsStrictlyGreaterThan() {
        Assert.False(MonsterSpellcasting.ClearsHealthGate(health: 10, bracket: 0));
        Assert.True(MonsterSpellcasting.ClearsHealthGate(health: 11, bracket: 0));
    }

    [Fact]
    public void AnUnusedBracketWouldOnlyMeanAlive() {
        Assert.True(MonsterSpellcasting.ClearsHealthGate(health: 1, bracket: 5));
        Assert.False(MonsterSpellcasting.ClearsHealthGate(health: 0, bracket: 5));
    }

    [Fact]
    public void ABracketOffTheTableIsRefusedRatherThanReadingPastIt() {
        Assert.False(MonsterSpellcasting.ClearsHealthGate(health: 99, bracket: 8));
        Assert.False(MonsterSpellcasting.ClearsHealthGate(health: 99, bracket: -1));
    }

    [Fact]
    public void TheTurnGateAndTheActionGateMeasureDifferentPools() {
        // Plenty of stamina and almost no health passes the turn gate and fails every action gate.
        Assert.True(MonsterSpellcasting.WellEnoughToAct(healthStaminaCombined: 40));
        Assert.False(MonsterSpellcasting.ClearsHealthGate(health: 3, bracket: 0));
    }
}
