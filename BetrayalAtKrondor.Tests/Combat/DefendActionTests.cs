namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>The Defend button. It heals, it ends the turn, and it does not set Parry.</summary>
public class DefendActionTests {
    private static Combatant Actor() => new Combatant { Flags = CombatantFlags.Ready };

    [Fact]
    public void DefendSetsDefendCommand_NotParry() {
        // *** The conflation to avoid. *** Only Parry feeds the melee to-hit penalty; treating them
        // as one would hand every defending character a bonus the original never gives.
        Combatant a = Actor();
        DefendAction.Apply(a, recovers: true, maxHealth: 60, maxStamina: 60);

        Assert.True((a.Flags & CombatantFlags.DefendCommand) != 0);
        Assert.True((a.Flags & CombatantFlags.Parry) == 0);
        Assert.Equal(CombatantFlags.DefendCommand, DefendAction.FlagSet);
    }

    [Fact]
    public void DefendingEndsTheTurn() {
        Combatant a = Actor();
        DefendAction.Apply(a, recovers: true, 60, 60);

        Assert.True((a.Flags & CombatantFlags.Ready) == 0, "a commitment, not a free stance");
    }

    [Fact]
    public void RecoveryComesOffTheCEILINGS_NotTheCurrentValues() {
        // So a badly wounded character recovers the same as a fresh one: the rate depends on who you
        // are, not on how hurt you are.
        Assert.Equal(4, DefendAction.HealAmount(maxHealth: 60, maxStamina: 60));
        Assert.Equal(4, DefendAction.HealAmount(60, 60));
    }

    [Fact]
    public void RecoveryIsAtLeastOne() {
        // Any character whose combined maxima are under 30 would otherwise recover nothing and
        // defending would be a wasted turn for them.
        Assert.Equal(1, DefendAction.HealAmount(maxHealth: 5, maxStamina: 5));
        Assert.Equal(1, DefendAction.HealAmount(0, 0));
        Assert.Equal(30, DefendAction.HealDivisor);
    }

    [Fact]
    public void AGatedCharacterStillDefendsButRecoversNothing() {
        Combatant a = Actor();
        int healed = DefendAction.Apply(a, recovers: false, 60, 60);

        Assert.Equal(0, healed);
        Assert.True((a.Flags & CombatantFlags.DefendCommand) != 0, "the stance still applies");
        Assert.True((a.Flags & CombatantFlags.Ready) == 0, "and the turn is still spent");
    }

    [Fact]
    public void TheRecoveryTargetsTheCombinedAttribute() {
        // stat_combatant_modify(actor, 0x10, ...) - attribute 16, not Health or Stamina alone.
        Assert.Equal(ActorAttribute.HealthStaminaCombo, DefendAction.HealedAttribute);
        Assert.Equal(16, (int)DefendAction.HealedAttribute);
    }

    [Fact]
    public void TheRecoveryIsCappedAtEightyPercent() {
        // stat_combatant_modify's fourth argument is a CAP, not a duration - so defending
        // repeatedly never reaches full health by that route alone.
        Assert.Equal(80, DefendAction.HealCapPercent);
    }

    [Fact]
    public void TheButtonHasAHelpDialog() {
        // Every combat button has a preview branch that plays a dialog and returns without acting.
        Assert.Equal(0x107, DefendAction.HelpDialog);
    }
}
