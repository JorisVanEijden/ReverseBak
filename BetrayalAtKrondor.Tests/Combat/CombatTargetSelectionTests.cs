namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// What a click does while target selection is armed — the seam Shoot and Cast share.
/// </summary>
public class CombatTargetSelectionTests {
    private const int OffGrid = SpellTargetingRules.OffGridDistance;
    private const int OnField = SpellTargetingRules.FieldBottomY - 1;
    private const int InMenuBar = SpellTargetingRules.FieldBottomY;
    private const int LivingActorType = 0;

    [Fact]
    public void AMissedClickLeavesTheModeArmed() {
        // *** No rejected click is a cancel. *** Every one of these leaves the player still choosing;
        // treating any as a cancel would silently throw away the command they pressed.
        Assert.Equal(CombatTargetSelection.Resolution.Pending,
            CombatTargetSelection.Resolve(confirmed: false, 1, OnField, false, true, LivingActorType));
        Assert.Equal(CombatTargetSelection.Resolution.Pending,
            CombatTargetSelection.Resolve(true, OffGrid, OnField, false, true, LivingActorType));
        Assert.Equal(CombatTargetSelection.Resolution.Pending,
            CombatTargetSelection.Resolve(true, 1, InMenuBar, false, true, LivingActorType));
    }

    [Fact]
    public void TheCapabilityPicksTheArm_NotTheButtonThatWasPressed() {
        // Same click, same target, same spell record: whether it is a shot or a cast is decided by
        // whether the actor could shoot at THIS moment.
        Assert.Equal(CombatTargetSelection.Resolution.Shoot,
            CombatTargetSelection.Resolve(true, 3, OnField, actorCanShoot: true, hasTarget: true,
                LivingActorType));
        Assert.Equal(CombatTargetSelection.Resolution.CastAtTarget,
            CombatTargetSelection.Resolve(true, 3, OnField, actorCanShoot: false, hasTarget: true,
                LivingActorType));
    }

    [Fact]
    public void OnlyACrystalSpellCommitsOnAnEmptyCell() {
        Assert.Equal(CombatTargetSelection.Resolution.CastAtTarget,
            CombatTargetSelection.Resolve(true, 3, OnField, false, hasTarget: false,
                CombatTargetSelection.CrystalTargetingType));
        // Every other actor-aimed type falls through to movement instead.
        Assert.Equal(CombatTargetSelection.Resolution.RevertToMove,
            CombatTargetSelection.Resolve(true, 3, OnField, false, hasTarget: false, LivingActorType));
    }

    [Fact]
    public void GroundAimedType6SkipsTheCapabilityGuardThat5Has() {
        // *** Faithful port of an asymmetry in the original. *** Both types aim at ground, but only
        // type 5 is guarded on the actor being unable to shoot. If this ever "gets fixed" the two
        // lines below stop disagreeing, and that is the signal to go back to COMBAT.C:2423.
        Assert.Equal(CombatTargetSelection.Resolution.RevertToMove,
            CombatTargetSelection.Resolve(true, 3, OnField, actorCanShoot: true, hasTarget: false,
                CombatTargetSelection.GroundTargetingType));
        Assert.Equal(CombatTargetSelection.Resolution.CastAtGround,
            CombatTargetSelection.Resolve(true, 3, OnField, actorCanShoot: true, hasTarget: false,
                CombatTargetSelection.SummonTargetingType));
    }

    [Fact]
    public void RevertingToMovementDoesNotSpendTheTurn() {
        Assert.False(CombatTargetSelection.SpendsTheTurn(CombatTargetSelection.Resolution.RevertToMove));
        Assert.False(CombatTargetSelection.SpendsTheTurn(CombatTargetSelection.Resolution.Pending));
        Assert.True(CombatTargetSelection.SpendsTheTurn(CombatTargetSelection.Resolution.Shoot));
        Assert.True(CombatTargetSelection.SpendsTheTurn(CombatTargetSelection.Resolution.CastAtTarget));
        Assert.True(CombatTargetSelection.SpendsTheTurn(CombatTargetSelection.Resolution.CastAtGround));
    }

    [Fact]
    public void ACorpseIsNotAShootableTarget() {
        Assert.False(CombatTargetSelection.ShotIsValid(targetIsEncounterActor: true, targetIsDead: true,
            targetIsInLineOfFire: true, hasSelectedQuarrel: true));
    }

    [Fact]
    public void ShootingNeedsLineOfFire_AnEnemy_AndAmmunition() {
        Assert.True(CombatTargetSelection.ShotIsValid(true, false, true, true));
        // Empty ground never carries the line-of-fire bit, so it fails here first.
        Assert.False(CombatTargetSelection.ShotIsValid(true, false, targetIsInLineOfFire: false, true));
        // Your own party is not a target.
        Assert.False(CombatTargetSelection.ShotIsValid(targetIsEncounterActor: false, false, true, true));
        // The chosen kind ran out between opening the menu and clicking.
        Assert.False(CombatTargetSelection.ShotIsValid(true, false, true, hasSelectedQuarrel: false));
    }
}
