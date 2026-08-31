namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which melee attack a monster makes — <c>combatenc_ai_melee_pick</c> (CBENC.C:829).
/// </summary>
public class MonsterMeleeChoiceTests {
    [Fact]
    public void ItRollsAgainstItsOwnSwingAccuracyRatherThanFlippingACoin() {
        // A creature that would probably miss with the heavy attack makes the light one.
        Assert.Equal(CombatActionDispatch.MeleeAttack.Swing,
            MonsterMeleeChoice.Pick(roll: 10, accuracyMelee: 40, swingAccuracy: 20, speedLeft: 5));
        Assert.Equal(CombatActionDispatch.MeleeAttack.Thrust,
            MonsterMeleeChoice.Pick(roll: 90, accuracyMelee: 40, swingAccuracy: 20, speedLeft: 5));
    }

    [Fact]
    public void AnExactRollStillSwings() {
        // `if (rand_roll > total) thrust` -- strictly greater loses, so equality swings.
        Assert.Equal(CombatActionDispatch.MeleeAttack.Swing,
            MonsterMeleeChoice.Pick(roll: 60, accuracyMelee: 40, swingAccuracy: 20, speedLeft: 5));
        Assert.Equal(CombatActionDispatch.MeleeAttack.Thrust,
            MonsterMeleeChoice.Pick(roll: 61, accuracyMelee: 40, swingAccuracy: 20, speedLeft: 5));
    }

    [Fact]
    public void ATurnWithNoSpeedLeftThrustsHoweverGoodTheRoll() {
        Assert.Equal(CombatActionDispatch.MeleeAttack.Thrust,
            MonsterMeleeChoice.Pick(roll: 0, accuracyMelee: 99, swingAccuracy: 99, speedLeft: 1));
        Assert.Equal(CombatActionDispatch.MeleeAttack.Swing,
            MonsterMeleeChoice.Pick(roll: 0, accuracyMelee: 99, swingAccuracy: 99,
                speedLeft: MonsterMeleeChoice.SwingMinimumSpeed));
    }
}

/// <summary>
/// Which weapon fields each melee attack reads — the pairing a swapped pair of canassa function
/// names made look crossed.
/// </summary>
public class MeleeAttackFieldPairingTests {
    private const int SwingAccuracy = 11;
    private const int ThrustAccuracy = 22;
    private const int SwingBase = 33;
    private const int ThrustBase = 44;

    [Fact]
    public void EachAttackReadsItsOwnAccuracyAndItsOwnDamage() {
        // combat_arena_melee_attack (the SWING) rolls nDefense_or_range_close and does
        // nSwing_damage; resolve_melee_swing (the THRUST) rolls nAttack_or_range_long and does
        // nThrust_damage. Taking the to-hit from one body and the damage from the other -- which is
        // what our port did -- produces an attack that does not exist.
        Assert.Equal(SwingAccuracy, CombatActionDispatch.AccuracyOf(
            CombatActionDispatch.MeleeAttack.Swing, SwingAccuracy, ThrustAccuracy));
        Assert.Equal(SwingBase, CombatActionDispatch.DamageBaseOf(
            CombatActionDispatch.MeleeAttack.Swing, SwingBase, ThrustBase));
        Assert.Equal(ThrustAccuracy, CombatActionDispatch.AccuracyOf(
            CombatActionDispatch.MeleeAttack.Thrust, SwingAccuracy, ThrustAccuracy));
        Assert.Equal(ThrustBase, CombatActionDispatch.DamageBaseOf(
            CombatActionDispatch.MeleeAttack.Thrust, SwingBase, ThrustBase));
    }

    [Fact]
    public void TheHeavyAttackIsTwiceAsHardOnTheWeapon() {
        Assert.Equal(CombatFormulas.WeaponWearOnSwing, CombatActionDispatch.WearSeverityOf(
            CombatActionDispatch.MeleeAttack.Swing));
        Assert.Equal(CombatFormulas.WeaponWearOnThrust, CombatActionDispatch.WearSeverityOf(
            CombatActionDispatch.MeleeAttack.Thrust));
        Assert.Equal(CombatFormulas.WeaponWearOnSwing, CombatFormulas.WeaponWearOnThrust * 2);
    }

    [Fact]
    public void TheLeftButtonThrustsAndTheRightSwings() {
        // The panel's own bottom row says so: "Left" under the Thrust column, "Right" under Swing
        // (combat_arena_hud_melee_panel). Nothing else on screen does.
        Assert.Equal(CombatActionDispatch.MeleeAttack.Thrust,
            CombatActionDispatch.AttackFor(CombatActionDispatch.LeftButton));
        Assert.Equal(CombatActionDispatch.MeleeAttack.Swing,
            CombatActionDispatch.AttackFor(CombatActionDispatch.RightButton));
    }
}
