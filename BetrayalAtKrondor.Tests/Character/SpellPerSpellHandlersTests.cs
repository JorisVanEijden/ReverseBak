namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The sixteen spells with a handler of their own. Four discard the number the calculation just
/// produced, one is a die roll, and one drains through a divide.
/// </summary>
public class SpellPerSpellHandlersTests {
    [Fact]
    public void MostSpellsFallStraightThrough() {
        Assert.False(SpellPerSpellHandlers.HasHandler(SpellIds.Skyfire));
        Assert.False(SpellPerSpellHandlers.HasHandler(SpellIds.Stardusk));
        Assert.False(SpellPerSpellHandlers.HasHandler(SpellIds.Firestorm));
    }

    [Fact]
    public void SixteenDoNot() {
        foreach (int id in new[] {
                     SpellIds.DespairThyEyes, SpellIds.HochosHaven, SpellIds.BaneOfBlackSlayers,
                     SpellIds.Nightfingers, SpellIds.GriefOfAThousandNights, SpellIds.Mirrorwall,
                     SpellIds.TouchOfLimsKragma, SpellIds.UnfortunateFlux, SpellIds.MadGodsRage,
                     SpellIds.SkinOfTheDragon, SpellIds.Steelfire, SpellIds.WindsOfEortis,
                     SpellIds.Invitation, SpellIds.BlackNimbus, SpellIds.StrengthDrain,
                     SpellIds.EvilSeek,
                 }) {
            Assert.True(SpellPerSpellHandlers.HasHandler(id));
        }
    }

    [Fact]
    public void ThreeHandlersAreNothingButASound() {
        Assert.True(SpellPerSpellHandlers.HandlerIsSoundOnly(SpellIds.SkinOfTheDragon));
        Assert.True(SpellPerSpellHandlers.HandlerIsSoundOnly(SpellIds.HochosHaven));
        Assert.True(SpellPerSpellHandlers.HandlerIsSoundOnly(SpellIds.UnfortunateFlux));
        Assert.False(SpellPerSpellHandlers.HandlerIsSoundOnly(SpellIds.Steelfire));
    }

    [Fact]
    public void StrengthDrainAndEvilSeekDiscardTheirOwnMagnitude() {
        Assert.True(SpellPerSpellHandlers.ZeroesMagnitude(SpellIds.StrengthDrain));
        Assert.True(SpellPerSpellHandlers.ZeroesMagnitude(SpellIds.EvilSeek));
        Assert.False(SpellPerSpellHandlers.ZeroesMagnitude(SpellIds.Steelfire));
    }

    [Fact]
    public void BaneOfBlackSlayersDiscardsItsMagnitudeAgainstAnythingElse() {
        // Its record says 5 damage against a 10-15 cost; that 50-75 lands on exactly one creature.
        Assert.False(SpellPerSpellHandlers.ZeroesMagnitude(SpellIds.BaneOfBlackSlayers,
            targetIsBlackSlayer: true));
        Assert.True(SpellPerSpellHandlers.ZeroesMagnitude(SpellIds.BaneOfBlackSlayers,
            targetIsBlackSlayer: false));
    }

    [Fact]
    public void StrengthDrainTakesExactlyTheCostInvested() {
        // The shipped damage field is -1, so the negation makes the divisor 1.
        Assert.Equal(10, SpellPerSpellHandlers.StrengthDrained(spellCost: 10, damage: -1));
        Assert.Equal(20, SpellPerSpellHandlers.StrengthDrained(spellCost: 20, damage: -1));
    }

    [Fact]
    public void AndItDividesSoARaisedDamageFieldWeakensIt() {
        // Backwards from every other spell: the field named damage is a divisor here.
        Assert.Equal(5, SpellPerSpellHandlers.StrengthDrained(spellCost: 20, damage: 4));
        Assert.Equal(4, SpellPerSpellHandlers.StrengthDrained(spellCost: 20, damage: -5));
    }

    [Fact]
    public void AZeroDamageFieldWouldDivideByZero() {
        // Same blind spot as the other two divisions; answered rather than faulted.
        Assert.Equal(0, SpellPerSpellHandlers.StrengthDrained(spellCost: 20, damage: 0));
    }

    [Fact]
    public void DespairHitsMonstersPermanentlyAndPartyMembersOnATimer() {
        Assert.True(SpellPerSpellHandlers.DespairIsPermanentFor(0));
        Assert.False(SpellPerSpellHandlers.DespairIsPermanentFor(1));
        Assert.False(SpellPerSpellHandlers.DespairIsPermanentFor(6));
    }

    [Fact]
    public void AndItTakesTwentyFromAllThreeAccuracies() {
        Assert.Equal(-20, SpellPerSpellHandlers.DespairAccuracyPenalty);
        Assert.Equal(3, SpellPerSpellHandlers.DespairAttributes.Length);
        Assert.Contains(ActorAttribute.AccuracyCasting, SpellPerSpellHandlers.DespairAttributes);
        Assert.Contains(ActorAttribute.AccuracyMelee, SpellPerSpellHandlers.DespairAttributes);
        Assert.Contains(ActorAttribute.AccuracyCrossbow, SpellPerSpellHandlers.DespairAttributes);
    }

    [Fact]
    public void ATimedModifierIsRefusedByAForeignHolderOfTheSameAttribute() {
        var kinds = new[] { 0x0055, 0, 0, 0, 0, 0, 0, 0 };
        var flags = new ActorAttributeFlag[8];
        flags[0] = ActorAttributeFlag.AccuracyMelee;
        Assert.False(SpellPerSpellHandlers.TimedModifierAccepted(kinds, flags,
            ActorAttribute.AccuracyMelee));
    }

    [Fact]
    public void ButNotByOneOfItsOwnKind() {
        var kinds = new[] { SpellPerSpellHandlers.TimedModifierKind, 0, 0, 0, 0, 0, 0, 0 };
        var flags = new ActorAttributeFlag[8];
        flags[0] = ActorAttributeFlag.AccuracyMelee;
        Assert.True(SpellPerSpellHandlers.TimedModifierAccepted(kinds, flags,
            ActorAttribute.AccuracyMelee));
    }

    [Fact]
    public void NorByAForeignHolderOfADifferentAttribute() {
        var kinds = new[] { 0x0055, 0, 0, 0, 0, 0, 0, 0 };
        var flags = new ActorAttributeFlag[8];
        flags[0] = ActorAttributeFlag.Strength;
        Assert.True(SpellPerSpellHandlers.TimedModifierAccepted(kinds, flags,
            ActorAttribute.AccuracyMelee));
    }

    [Fact]
    public void GriefExemptsTwelveCreatureTypesAndAffectsEverythingElse() {
        Assert.False(SpellPerSpellHandlers.GriefAffects(28));
        Assert.False(SpellPerSpellHandlers.GriefAffects(44));
        Assert.False(SpellPerSpellHandlers.GriefAffects(58));
        Assert.True(SpellPerSpellHandlers.GriefAffects(29));
        Assert.True(SpellPerSpellHandlers.GriefAffects(45));
    }

    [Fact]
    public void IncludingEveryCreatureBelowTheSwitchesRange() {
        // Types under 28 fall out of range and take the default, which is "affected" — the
        // exemption list is the explicit part, not the eligibility.
        Assert.True(SpellPerSpellHandlers.GriefAffects(0));
        Assert.True(SpellPerSpellHandlers.GriefAffects(27));
        Assert.True(SpellPerSpellHandlers.GriefAffects(200));
    }

    [Fact]
    public void BlackNimbusIsTenPercentPlusSevenPerPointOfCost() {
        Assert.Equal(17, SpellPerSpellHandlers.BlackNimbusChancePercent(1));
        Assert.Equal(80, SpellPerSpellHandlers.BlackNimbusChancePercent(10));
    }

    [Fact]
    public void AndIsNeverCertainAcrossItsWholeCostRange() {
        // Cost runs 1..10; even a maximum cast fails one roll in five.
        for (int cost = 1; cost <= 10; cost++) {
            Assert.True(SpellPerSpellHandlers.BlackNimbusChancePercent(cost) < 100);
        }
    }

    [Fact]
    public void TheRollIsUnderNotUnderOrEqual() {
        Assert.True(SpellPerSpellHandlers.BlackNimbusSucceeds(rollUnder100: 16, spellCost: 1));
        Assert.False(SpellPerSpellHandlers.BlackNimbusSucceeds(rollUnder100: 17, spellCost: 1));
    }
}
