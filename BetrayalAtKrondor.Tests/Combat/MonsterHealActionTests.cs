namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Action slot 1 — heal an ally. It bypasses the spell selector entirely, names two spells by
/// number, and asks its urgency question of the wrong ally.
/// </summary>
public class MonsterHealActionTests {
    [Fact]
    public void TheHealSlotNamesTwoSpellsTheSelectorCouldNeverReturn() {
        // Gift of Sung is targeting type 2 and Hocho's Haven type 3, so neither passes the
        // martial-plus-type-0-or-1 filter every other slot uses.
        Assert.False(MonsterSpellcasting.InMonsterRepertoire(MonsterSpellcasting.GiftOfSung,
            isMartial: true, targetingType: 2));
        Assert.False(MonsterSpellcasting.InMonsterRepertoire(MonsterSpellcasting.HochosHaven,
            isMartial: true, targetingType: 3));
    }

    [Fact]
    public void SoTheRepertoireIsSeventeenNotFifteen() {
        Assert.True(MonsterSpellcasting.CastableByAMonster(MonsterSpellcasting.GiftOfSung,
            isMartial: true, targetingType: 2));
        Assert.True(MonsterSpellcasting.CastableByAMonster(MonsterSpellcasting.HochosHaven,
            isMartial: true, targetingType: 3));
        Assert.True(MonsterSpellcasting.CastableByAMonster(SpellIds.Skyfire,
            isMartial: true, targetingType: 1));
    }

    [Fact]
    public void AndStillExcludesEverythingElse() {
        Assert.False(MonsterSpellcasting.CastableByAMonster(SpellIds.FinalRest,
            isMartial: true, targetingType: 7));
        Assert.False(MonsterSpellcasting.CastableByAMonster(SpellIds.Nightfingers,
            isMartial: false, targetingType: 4));
    }

    [Fact]
    public void AHurtAllyDrawsTheRealHeal() {
        Assert.Equal(MonsterSpellcasting.GiftOfSung, MonsterSpellcasting.ChooseHealSpell(
            allyHealthConsulted: 10, urgencyRoll: 60, giftOfSungAvailable: true,
            hochosHavenAvailable: true, targetAlreadyHasHochosHaven: false));
    }

    [Fact]
    public void ButOnlyProbablyBecauseTheThresholdIsARoll() {
        // The same ally, a low roll, and it falls through to the lingering effect instead.
        Assert.Equal(MonsterSpellcasting.HochosHaven, MonsterSpellcasting.ChooseHealSpell(
            allyHealthConsulted: 10, urgencyRoll: 5, giftOfSungAvailable: true,
            hochosHavenAvailable: true, targetAlreadyHasHochosHaven: false));
    }

    [Fact]
    public void AnUnavailableGiftOfSungFallsBackRatherThanFailing() {
        Assert.Equal(MonsterSpellcasting.HochosHaven, MonsterSpellcasting.ChooseHealSpell(
            allyHealthConsulted: 10, urgencyRoll: 60, giftOfSungAvailable: false,
            hochosHavenAvailable: true, targetAlreadyHasHochosHaven: false));
    }

    [Fact]
    public void AndAnAllyThatAlreadyHasTheEffectGetsNothing() {
        Assert.Equal(-1, MonsterSpellcasting.ChooseHealSpell(
            allyHealthConsulted: 90, urgencyRoll: 60, giftOfSungAvailable: true,
            hochosHavenAvailable: true, targetAlreadyHasHochosHaven: true));
    }

    [Fact]
    public void NeitherAvailableMeansNoAction() {
        Assert.Equal(-1, MonsterSpellcasting.ChooseHealSpell(
            allyHealthConsulted: 10, urgencyRoll: 60, giftOfSungAvailable: false,
            hochosHavenAvailable: false, targetAlreadyHasHochosHaven: false));
    }

    [Fact]
    public void AHealTargetIsAliveAndBelowFull() {
        Assert.False(MonsterSpellcasting.IsHealTarget(0));
        Assert.True(MonsterSpellcasting.IsHealTarget(1));
        Assert.True(MonsterSpellcasting.IsHealTarget(99));
        Assert.False(MonsterSpellcasting.IsHealTarget(100));
    }

    [Fact]
    public void AMonsterNeverHealsItself() {
        // A wounded lone caster falls through to the next action in its pattern row instead.
        Assert.False(MonsterSpellcasting.HealsSelf);
    }

    [Fact]
    public void TheUrgencyQuestionIsAskedOfTheWrongAlly() {
        // The loop keeps a running minimum and never reads it; the decision uses the last ally
        // examined. Verified from the encoded displacements.
        Assert.True(MonsterSpellcasting.HealUrgencyReadsTheLastAllyNotTheWorst);
    }

    [Fact]
    public void AndTheSpellIsPickedBeforeTheTargetIs() {
        Assert.True(MonsterSpellcasting.HealSpellIsChosenBeforeTheTarget);
        Assert.True(MonsterSpellcasting.HealsOneAllyPerAction);
    }
}
