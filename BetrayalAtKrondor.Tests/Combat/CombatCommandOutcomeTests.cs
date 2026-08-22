namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which combat buttons resolve on the spot and which only arm a mode for the next click.
/// </summary>
public class CombatCommandOutcomeTests {
    [Fact]
    public void ShootAndInspectARMRatherThanResolve() {
        // *** The split a port misses. *** Treating these as immediate would fire a shot or resolve
        // an inspection the moment the button is pressed, before a target has been picked.
        Assert.True(CombatCommandOutcome.ArmsAPendingMode(CombatCommands.Command.Shoot));
        Assert.True(CombatCommandOutcome.ArmsAPendingMode(CombatCommands.Command.Inspect));
        Assert.False(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.Shoot));
        Assert.False(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.Inspect));
    }

    [Fact]
    public void RestDefendAndAutoResolveSpendTheTurn() {
        Assert.True(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.Rest));
        Assert.True(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.Defend));
        Assert.True(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.AutoResolve));
    }

    [Fact]
    public void ACommandNeverBothSpendsTheTurnAndArmsAMode() {
        // The arena clears any pending mode when it resolves a turn, so the two are exclusive.
        foreach (CombatCommands.Command c in System.Enum.GetValues(typeof(CombatCommands.Command))) {
            Assert.False(CombatCommandOutcome.SpendsTheTurn(c) && CombatCommandOutcome.ArmsAPendingMode(c),
                c + " must not do both");
        }
    }

    [Fact]
    public void TheModeNumbersAreTheOriginals() {
        Assert.Equal(3, (int)CombatCommandOutcome.PendingMode.InspectTarget);
        Assert.Equal(4, (int)CombatCommandOutcome.PendingMode.ShootMenu);
        Assert.Equal(-1, (int)CombatCommandOutcome.PendingMode.None);
    }

    [Fact]
    public void TheLabelAndTheCharacterScreenDoNeither() {
        // Neither touches the turn: the label is not clickable at all, and the character screen is a
        // suspend-and-return, not an action.
        Assert.False(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.CapabilityLabel));
        Assert.False(CombatCommandOutcome.ArmsAPendingMode(CombatCommands.Command.CharacterScreen));
    }
}
