namespace BetrayalAtKrondor.Tests.Character;

using System.Collections.Generic;
using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The two delegated cast routines read so far. Strength Drain turns out to be a transfer, and
/// Steelfire enchants somebody else's sword.
/// </summary>
public class SpellCastRoutinesTests {
    [Fact]
    public void TheDrainIsClampedToWhatTheTargetStillHas() {
        Assert.Equal(12, SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 12));
        Assert.Equal(20, SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 60));
    }

    [Fact]
    public void AndTheCasterBanksHalfOfWhatWasActuallyTaken() {
        // Not half of what was asked for — draining a spent target is nearly worthless.
        int actual = SpellCastRoutines.ActualDrain(requested: 20, targetCurrentStrength: 4);
        Assert.Equal(2, SpellCastRoutines.CasterGain(actual));
    }

    [Fact]
    public void AWindElementalAtOrBelowTheDrainDiesInstead() {
        Assert.True(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 8, drain: 20));
        Assert.True(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 20, drain: 20));
        Assert.False(SpellCastRoutines.DrainKillsOutright(
            SpellCastRoutines.WindElementalCreatureType, targetCurrentStrength: 21, drain: 20));
    }

    [Fact]
    public void AndNothingElseDoes() {
        Assert.False(SpellCastRoutines.DrainKillsOutright(
            creatureType: 12, targetCurrentStrength: 1, drain: 99));
    }

    [Fact]
    public void TheWindElementalIsAlsoImmuneToGrief() {
        // Corroborates the creature number read from the compare's bytes: 54 sits inside the band
        // Grief of 1000 Nights exempts.
        Assert.False(
            SpellPerSpellHandlers.GriefAffects(SpellCastRoutines.WindElementalCreatureType));
    }

    [Fact]
    public void AMonsterCasterBanksHalfWhatAPartyCasterDoes() {
        // The gain paths disagree on scale — 128 against the 256 the loss paths use.
        Assert.Equal(10, SpellCastRoutines.CasterGain(20));
        Assert.Equal(5, SpellCastRoutines.PermanentCasterGainPoints(20));
    }

    [Fact]
    public void SteelfireFindsTheFirstEquippedSword() {
        var objects = BuildObjects();
        var container = BuildContainer(
            (objectId: 30, flags: (ushort)ItemFlags.Equipped),      // armor, equipped
            (objectId: 20, flags: 0),                                // sword, not equipped
            (objectId: 20, flags: (ushort)ItemFlags.Equipped));      // sword, equipped

        Assert.Equal(2, SpellCastRoutines.SteelfireTarget(container, objects));
    }

    [Fact]
    public void AndFindsNothingWhenNoSwordIsWorn() {
        var objects = BuildObjects();
        var container = BuildContainer(
            (objectId: 30, flags: (ushort)ItemFlags.Equipped),
            (objectId: 20, flags: 0));

        Assert.Equal(-1, SpellCastRoutines.SteelfireTarget(container, objects));
    }

    [Fact]
    public void WhichStillCostsTheCaster() {
        Assert.True(SpellCastRoutines.SteelfireChargesEvenWhenItFindsNothing);
    }

    [Fact]
    public void TheEnchantmentIsOredInAndLeavesOtherFlagsAlone() {
        ushort before = (ushort)(ItemFlags.Equipped | ItemFlags.Flaming);
        ushort after = SpellCastRoutines.ApplySteelfire(before);

        Assert.Equal((ushort)ItemFlags.SteelFired, (ushort)(after & (ushort)ItemFlags.SteelFired));
        Assert.Equal((ushort)ItemFlags.Equipped, (ushort)(after & (ushort)ItemFlags.Equipped));
        Assert.Equal((ushort)ItemFlags.Flaming, (ushort)(after & (ushort)ItemFlags.Flaming));
    }

    [Fact]
    public void AndIsIdempotent() {
        ushort once = SpellCastRoutines.ApplySteelfire((ushort)ItemFlags.Equipped);
        Assert.Equal(once, SpellCastRoutines.ApplySteelfire(once));
    }

    [Fact]
    public void AnAbsentTargetIsNotAnError() {
        Assert.Equal(-1, SpellCastRoutines.SteelfireTarget(null, BuildObjects()));
    }

    [Fact]
    public void NightfingersDoesNothingIfThePlayerTakesNothing() {
        Assert.False(SpellCastRoutines.NightfingersStoleSomething(itemsBefore: 4, itemsAfter: 4));
        Assert.True(SpellCastRoutines.NightfingersStoleSomething(itemsBefore: 4, itemsAfter: 5));
    }

    [Fact]
    public void TheGloryHandIsBurnedEitherWay() {
        // The record's ObjectId field names the consumable, and SpellCasting already refuses the
        // cast without it — which is why the routine destroys it without checking it found one.
        Assert.Equal(10, SpellCastRoutines.GloryHandObjectId);
    }

    [Fact]
    public void InvitationPullsNoFurtherThanTheTargetActuallyIs() {
        Assert.Equal(3, SpellCastRoutines.InvitationPull(chebyshevDistance: 3, power: 9));
    }

    [Fact]
    public void AndNoFurtherThanThePowerAllows() {
        // A weak Invitation drags a distant target only part of the way — not a teleport.
        Assert.Equal(2, SpellCastRoutines.InvitationPull(chebyshevDistance: 7, power: 2));
    }

    [Fact]
    public void EvilSeekStartsAtTwiceTheCost() {
        Assert.Equal(60, SpellCastRoutines.EvilSeekInitialPower(30));
    }

    [Fact]
    public void TheFirstHopIsAtFullPower() {
        // The multiplier only drops to 80 after being applied once, so the original target takes
        // the undiminished figure.
        Assert.Equal(60, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 30, hop: 0));
    }

    [Fact]
    public void AndEachHopAfterKeepsFourFifthsOfTheOneBefore() {
        Assert.Equal(48, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 30, hop: 1));
        Assert.Equal(38, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 30, hop: 2));
        Assert.Equal(30, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 30, hop: 3));
    }

    [Fact]
    public void TheTruncationIsWhatEventuallyEndsTheChain() {
        // Integer division, not rounding: a chain started weak dies out rather than trailing off
        // into fractions forever.
        // A minimum-cost cast starts at 2 and is spent after two hops: 2 -> 1 -> 0.
        Assert.Equal(2, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 1, hop: 0));
        Assert.Equal(1, SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 1, hop: 1));
        Assert.True(SpellCastRoutines.EvilSeekEndsAtZeroPower(
            SpellCastRoutines.EvilSeekPowerAtHop(spellCost: 1, hop: 2)));
    }

    [Fact]
    public void TheTwoSpellsThatEndTheCastAreTheTwoThatBillThemselves() {
        // So clearing the continue flag means "already charged", not "cancelled".
        Assert.False(SpellCastTail.EndingEarlyIsFree(SpellIds.WindsOfEortis));
        Assert.False(SpellCastTail.EndingEarlyIsFree(SpellIds.MadGodsRage));
    }

    [Fact]
    public void ASuppressedCastIsStillGenuinelyFree() {
        Assert.True(SpellCastTail.EndingEarlyIsFree(SpellIds.Skyfire));
        Assert.True(SpellCastTail.EndingEarlyIsFree(SpellIds.TouchOfLimsKragma));
    }

    [Fact]
    public void TheKnockbackRunsOneCellPerPointOfCost() {
        Assert.Equal(14, SpellCastRoutines.KnockbackCells(14));
    }

    [Fact]
    public void TheHorizontalStepFollowsTheCompass() {
        Assert.Equal(0, SpellCastRoutines.KnockbackDx(0));
        Assert.Equal(1, SpellCastRoutines.KnockbackDx(2));
        Assert.Equal(0, SpellCastRoutines.KnockbackDx(4));
        Assert.Equal(-1, SpellCastRoutines.KnockbackDx(6));
    }

    [Fact]
    public void TheVerticalStepDoesNotAtDirectionZero() {
        // 4 pushes one way and 0 should push the other; the original's branches let 0 fall through
        // to the no-movement arm while 1 and 7 are handled.
        Assert.Equal(1, SpellCastRoutines.KnockbackDy(4));
        Assert.Equal(-1, SpellCastRoutines.KnockbackDy(1));
        Assert.Equal(-1, SpellCastRoutines.KnockbackDy(7));
        Assert.Equal(0, SpellCastRoutines.KnockbackDy(0));
    }

    [Fact]
    public void SoAVictimDirectlyAlongDirectionZeroIsNotPushedAtAll() {
        Assert.True(SpellCastRoutines.KnockbackIsInert(0));
        for (int direction = 1; direction <= 7; direction++) {
            Assert.False(SpellCastRoutines.KnockbackIsInert(direction));
        }
    }

    [Fact]
    public void SixOfTheSevenAfflictionsBlockASpellHealEntirely() {
        foreach (ActorCondition condition in SpellCastRoutines.AfflictionsThatBlockHealing) {
            var conditions = new ActorConditions();
            conditions[condition] = 1;
            Assert.False(SpellCastRoutines.HealApplies(targetActorNumber: 2, conditions));
        }
    }

    [Fact]
    public void HealingIsTheOneDeliberatelyLeftOut() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Healing] = 50;
        Assert.True(SpellCastRoutines.HealApplies(targetActorNumber: 2, conditions));
        Assert.DoesNotContain(ActorCondition.Healing, SpellCastRoutines.AfflictionsThatBlockHealing);
    }

    [Fact]
    public void AMonsterIsAlwaysHealable() {
        // Tested before the afflictions are read at all — non-party actors have no row.
        var conditions = new ActorConditions();
        conditions[ActorCondition.Poisoned] = 90;
        Assert.True(SpellCastRoutines.HealApplies(targetActorNumber: 0, conditions));
    }

    [Fact]
    public void AHealthyPartyMemberIsHealed() {
        Assert.True(SpellCastRoutines.HealApplies(targetActorNumber: 3, new ActorConditions()));
    }

    [Fact]
    public void ThoughtsLikeCloudsBlocksTheHealBeforeTheCasterIsCharged() {
        // The one case where a type-2 delivery does not bill.
        Assert.True(SpellCastRoutines.HealIsBlockedForFree(casterHasThoughtsLikeClouds: true));
        Assert.True(SpellCastTail.CasterPays(costWasNegated: true, targetingType: 2));
    }

    [Fact]
    public void ASpellHealCannotPassFourFifthsOfTheCombinedPool() {
        var health = new ActorStat { Base = 50, Max = 50 };
        var stamina = new ActorStat { Base = 20, Max = 50 };
        int sum = StatEngine.ModifyHealthPool(health, stamina, 200L << 8,
            SpellCastRoutines.HealTargetPercent, out _);

        Assert.Equal(80, sum);
    }

    [Fact]
    public void AndDoesNothingAtAllForSomeoneAlreadyThere() {
        var health = new ActorStat { Base = 50, Max = 50 };
        var stamina = new ActorStat { Base = 45, Max = 50 };
        int sum = StatEngine.ModifyHealthPool(health, stamina, 200L << 8,
            SpellCastRoutines.HealTargetPercent, out _);

        Assert.Equal(95, sum);
    }

    [Fact]
    public void TheSpellHealAndTheRestHealAgreeOnTheCeiling() {
        // Reached two different ways — the rest heal fills and gives a fifth back, the spell heal
        // caps — and they land on the same number.
        Assert.Equal(CharacterHeal.PartialHealPercent, SpellCastRoutines.HealTargetPercent);
    }

    [Fact]
    public void TheFloatingNumberIsTheGainNegated() {
        // Healing shows negative, damage positive — the sign convention is shared.
        Assert.Equal(-12, SpellCastRoutines.HealFloatingNumber(
            healthBefore: 10, healthAfter: 18, staminaBefore: 4, staminaAfter: 8));
    }

    [Fact]
    public void AndABlockedHealStillFlashesAZero() {
        Assert.Equal(0, SpellCastRoutines.HealFloatingNumber(
            healthBefore: 10, healthAfter: 10, staminaBefore: 4, staminaAfter: 4));
    }

    [Fact]
    public void MadGodsRageChargesThreeHealthPerActorPerRound() {
        // Charged from inside the loop, which is why the spell also clears the continue flag.
        Assert.Equal(3, SpellCastRoutines.MadGodsRageCostPerActorPerRound);
        Assert.False(SpellCastTail.EndingEarlyIsFree(SpellIds.MadGodsRage));
    }

    [Fact]
    public void TheSurchargeRaisesMadGodsRageASecondTime() {
        // It already added half again to the cost in the dispatcher's prologue.
        Assert.Equal(15, SpellCastRoutines.MadGodsRageBase(surcharged: false));
        Assert.Equal(22, SpellCastRoutines.MadGodsRageBase(surcharged: true));
    }

    [Fact]
    public void TheExplosionAddsFiveOnTopOfTheRoll() {
        Assert.Equal(18, SpellCastRoutines.MadGodsRageDamage(
            surcharged: false, rollUnder5: 3, exploded: false));
        Assert.Equal(23, SpellCastRoutines.MadGodsRageDamage(
            surcharged: false, rollUnder5: 3, exploded: true));
    }

    [Fact]
    public void TheStrikeChanceIsNotACoinFlipAndShiftsWithTheFieldSize() {
        // count/2 + 1 out of count: four in six, but four in seven with one more actor present.
        int six = 0, seven = 0;
        for (int roll = 0; roll < 6; roll++) {
            if (SpellCastRoutines.MadGodsRageStrikes(6, roll)) { six++; }
        }
        for (int roll = 0; roll < 7; roll++) {
            if (SpellCastRoutines.MadGodsRageStrikes(7, roll)) { seven++; }
        }

        Assert.Equal(4, six);
        Assert.Equal(4, seven);
    }

    [Fact]
    public void AnEmptyFieldStrikesNobody() {
        Assert.False(SpellCastRoutines.MadGodsRageStrikes(0, 0));
    }

    [Fact]
    public void ThePoolCanHoldASpellNobodyCast() {
        // Four sites register an effect with zero cost and duration purely to borrow its
        // presentation, then remove the slot — so a pool entry is not proof of a cast.
        Assert.True(SpellCastRoutines.SpellsBorrowEachOthersIdentities);
        Assert.True(SpellCastRoutines.KnockbackWearsRiverSong);
    }

    private static ObjectInfoSet BuildObjects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("sw") {
            Number = 20, Name = "Broadsword", ObjectType = ObjectType.Sword,
            InventorySlots = 2, MaxAmount = 1,
        },
        new ObjectInfo("ar") {
            Number = 30, Name = "Armor", ObjectType = ObjectType.Armor,
            InventorySlots = 4, MaxAmount = 1,
        },
    });

    private static RuntimeContainer BuildContainer(params (byte objectId, ushort flags)[] items) {
        var container = new RuntimeContainer {
            Capacity = 24, ContainerType = SaveGameContainerType.Inventory,
        };
        foreach ((byte objectId, ushort flags) in items) {
            container.Items.Add(new RuntimeItem(objectId, 0, flags));
        }
        return container;
    }
}
