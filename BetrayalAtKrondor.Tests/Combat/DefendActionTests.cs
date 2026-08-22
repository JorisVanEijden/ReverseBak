namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The DEFEND button (id 32) — a guard, not the rest that heals.</summary>
public class DefendActionTests {
    private static Combatant Actor() => new Combatant { Flags = CombatantFlags.Ready };

    [Fact]
    public void DefendRaisesParryAndSpendsTheTurn() {
        Combatant a = Actor();
        DefendAction.Apply(a);

        Assert.True((a.Flags & CombatantFlags.Parry) != 0);
        Assert.True((a.Flags & CombatantFlags.Ready) == 0);
    }

    [Fact]
    public void DefendAndRestSetDIFFERENTFlags() {
        // *** The transposition this pair exists to prevent. *** They were the same command in our
        // model until the describe records separated them. Only Parry feeds the to-hit penalty.
        Combatant defender = Actor();
        Combatant rester = Actor();
        DefendAction.Apply(defender);
        RestAction.Apply(rester, recovers: true, maxHealth: 60, maxStamina: 60);

        Assert.Equal(CombatantFlags.Parry, DefendAction.FlagSet);
        Assert.Equal(CombatantFlags.DefendCommand, RestAction.FlagSet);
        Assert.NotEqual(DefendAction.FlagSet, RestAction.FlagSet);
        Assert.True((defender.Flags & CombatantFlags.DefendCommand) == 0);
        Assert.True((rester.Flags & CombatantFlags.Parry) == 0);
    }

    [Fact]
    public void DefendingActuallyMakesYouHarderToHit() {
        // Parry already has a consumer, so wiring Defend has an immediate effect rather than being
        // a flag nobody reads. The penalty applies to the ROLL, so the 2..98 clamp cannot eat it.
        const int chance = 50;
        Assert.True(CombatFormulas.MeleeHits(roll: 40, hitChance: chance, targetParrying: false));
        Assert.False(CombatFormulas.MeleeHits(roll: 40, hitChance: chance, targetParrying: true));
    }

    [Fact]
    public void DefendGivesNoRecovery() {
        // Unlike Rest, the routine is two flag operations - no heal, no roll, no animation.
        Combatant a = Actor();
        a.Health = 10;
        a.Stamina = 10;
        DefendAction.Apply(a);

        Assert.Equal(10, a.Health);
        Assert.Equal(10, a.Stamina);
    }

    [Fact]
    public void DefendingNobodyIsHarmless() {
        DefendAction.Apply(null);
    }

    [Fact]
    public void TheHelpRecordIsTheOneForIdThirtyTwo() {
        Assert.Equal(0x10a, DefendAction.HelpDialog);
        Assert.Equal(DefendAction.HelpDialog,
            CombatActionDispatch.HelpRecordFor(CombatCommands.DefendId));
    }
}
