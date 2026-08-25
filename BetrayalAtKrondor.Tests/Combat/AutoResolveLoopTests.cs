namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>What the auto-resolve button does, as opposed to what its old doc claimed.</summary>
public class AutoResolveLoopTests {
    [Fact]
    public void BackHandsControlBack() {
        Assert.True(AutoResolveLoop.Bails(AutoResolveLoop.BackMenuResult));
        Assert.True(AutoResolveLoop.Bails(AutoResolveLoop.CancelMenuResult));
        Assert.False(AutoResolveLoop.Bails(CombatCommands.RestId));
    }

    [Fact]
    public void TheBackIdMeansATHIRDThingHere() {
        // 33 already means "leave the fight" on the melee menu and "back out" on the shoot menu.
        // During auto-resolve it means "stop auto-resolving". A handler that routes it by which
        // menu is up still has to know whether the loop is running.
        Assert.Equal(CombatCommands.BackOrRetreatId, AutoResolveLoop.BackMenuResult);
    }

    [Fact]
    public void EitherSideBeingGoneEndsIt() {
        Assert.True(AutoResolveLoop.Finished(livingOnTheEnemySide: 0, livingOnThePartySide: 3));
        Assert.True(AutoResolveLoop.Finished(livingOnTheEnemySide: 2, livingOnThePartySide: 0));
        Assert.False(AutoResolveLoop.Finished(livingOnTheEnemySide: 2, livingOnThePartySide: 3));
    }

    [Fact]
    public void ItIsInterruptibleAndPlaysBothSides() {
        // The two facts a port gets wrong by reading the old one-line description: it does NOT run
        // to a winner, and there is no separate player AI.
        Assert.True(AutoResolveLoop.StopsOnlyOnAPartyTurn);
        Assert.True(AutoResolveLoop.PartyTurnsUseTheMonsterAiWithSidesSwapped);
    }
}
