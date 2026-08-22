namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Combat;
using Xunit;

/// <summary>The REST button (id 19). It heals, ends the turn, and does NOT set Parry.</summary>
public class RestActionTests {
    private static Combatant Actor() => new Combatant { Flags = CombatantFlags.Ready };

    [Fact]
    public void DefendSetsDefendCommand_NotParry() {
        // *** The conflation to avoid. *** Only Parry feeds the melee to-hit penalty; treating them
        // as one would hand every defending character a bonus the original never gives.
        Combatant a = Actor();
        RestAction.Apply(a, recovers: true, maxHealth: 60, maxStamina: 60);

        Assert.True((a.Flags & CombatantFlags.DefendCommand) != 0);
        Assert.True((a.Flags & CombatantFlags.Parry) == 0);
        Assert.Equal(CombatantFlags.DefendCommand, RestAction.FlagSet);
    }

    [Fact]
    public void DefendingEndsTheTurn() {
        Combatant a = Actor();
        RestAction.Apply(a, recovers: true, 60, 60);

        Assert.True((a.Flags & CombatantFlags.Ready) == 0, "a commitment, not a free stance");
    }

    [Fact]
    public void RecoveryComesOffTheCEILINGS_NotTheCurrentValues() {
        // So a badly wounded character recovers the same as a fresh one: the rate depends on who you
        // are, not on how hurt you are.
        Assert.Equal(4, RestAction.HealAmount(maxHealth: 60, maxStamina: 60));
        Assert.Equal(4, RestAction.HealAmount(60, 60));
    }

    [Fact]
    public void RecoveryIsAtLeastOne() {
        // Any character whose combined maxima are under 30 would otherwise recover nothing and
        // defending would be a wasted turn for them.
        Assert.Equal(1, RestAction.HealAmount(maxHealth: 5, maxStamina: 5));
        Assert.Equal(1, RestAction.HealAmount(0, 0));
        Assert.Equal(30, RestAction.HealDivisor);
    }

    [Fact]
    public void AGatedCharacterStillDefendsButRecoversNothing() {
        Combatant a = Actor();
        int healed = RestAction.Apply(a, recovers: false, 60, 60);

        Assert.Equal(0, healed);
        Assert.True((a.Flags & CombatantFlags.DefendCommand) != 0, "the stance still applies");
        Assert.True((a.Flags & CombatantFlags.Ready) == 0, "and the turn is still spent");
    }

    [Fact]
    public void TheRecoveryTargetsTheCombinedAttribute() {
        // stat_combatant_modify(actor, 0x10, ...) - attribute 16, not Health or Stamina alone.
        Assert.Equal(ActorAttribute.HealthStaminaCombo, RestAction.HealedAttribute);
        Assert.Equal(16, (int)RestAction.HealedAttribute);
    }

    [Fact]
    public void TheRecoveryIsCappedAtEightyPercent() {
        // stat_combatant_modify's fourth argument is a CAP, not a duration - so defending
        // repeatedly never reaches full health by that route alone.
        Assert.Equal(80, RestAction.HealCapPercent);
    }

    private static int[] NoConditions() => new int[ActorConditions.Count];

    [Fact]
    public void AHealthyCharacterRecovers() {
        Assert.True(RestAction.RecoveryAllowed(NoConditions()));
    }

    [Fact]
    public void EveryAfflictionBlocksTheRecovery() {
        foreach (ActorCondition c in new[] {
                ActorCondition.Sick, ActorCondition.Plagued, ActorCondition.Poisoned,
                ActorCondition.Drunk, ActorCondition.Starving, ActorCondition.NearDeath }) {
            int[] ranks = NoConditions();
            ranks[(int)c] = 1;
            Assert.False(RestAction.RecoveryAllowed(ranks), c + " must block the recovery");
        }
    }

    [Fact]
    public void BeingHEALEDDoesNotBlockIt() {
        // *** The whole shape of the rule. *** The original checks six of the seven slots and skips
        // exactly one - and the skipped one is Healing, the only entry that is a benefit rather than
        // an ailment. A port that tested "any condition set" would wrongly deny the recovery to a
        // character who is being healed.
        int[] ranks = NoConditions();
        ranks[(int)ActorCondition.Healing] = 50;
        Assert.True(RestAction.RecoveryAllowed(ranks));
    }

    [Fact]
    public void HealingIsTheFifthOfSeven_WhichIsWhyOffsetNineIsSkipped() {
        // The offsets read 5,6,7,8,10,11 - consecutive but for a gap where 9 would sit.
        Assert.Equal(4, (int)ActorCondition.Healing);
        Assert.Equal(7, ActorConditions.Count);
    }

    [Fact]
    public void AMonsterWithNoConditionRowRecovers() {
        // No character slot means the original skips the test entirely.
        Assert.True(RestAction.RecoveryAllowed(null));
    }

    [Fact]
    public void TheButtonHasAHelpDialog() {
        // Every combat button has a preview branch that plays a dialog and returns without acting.
        Assert.Equal(0x107, RestAction.HelpDialog);
    }
}
