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
        Assert.Equal(4, (int)CombatCommandOutcome.PendingMode.TargetSelection);
        Assert.Equal(1, (int)CombatCommandOutcome.PendingMode.CastCancelled);
        Assert.Equal(-1, (int)CombatCommandOutcome.PendingMode.None);
    }

    [Fact]
    public void CastArmsTargetingONLYIfASpellWasActuallyChosen() {
        // *** Corrected 2026-08-22, and it was backwards. *** Cast opens a MODAL spell picker
        // (cspell_cast_menu_loop) over the arena; only when it returns a real spell does targeting
        // begin. Backing out arms mode 1 and clears the pending selection instead.
        Assert.Equal(CombatCommandOutcome.PendingMode.TargetSelection,
            CombatCommandOutcome.ModeAfterCast(spellChosen: true));
        Assert.Equal(CombatCommandOutcome.PendingMode.CastCancelled,
            CombatCommandOutcome.ModeAfterCast(spellChosen: false));
    }

    [Fact]
    public void PressingCastAloneArmsNothing() {
        // The press opens the picker; it is the CHOICE that arms targeting. ModeFor cannot know
        // which happened, so it must not guess one.
        Assert.Equal(CombatCommandOutcome.PendingMode.None,
            CombatCommandOutcome.ModeFor(CombatCommands.Command.Cast));
        Assert.False(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.Cast));
    }

    [Fact]
    public void ShootAndAChosenSpellShareTheSameTargetingMode() {
        // Which is what names mode 4: calling it "the shoot menu" would miss the spell half.
        Assert.Equal(CombatCommandOutcome.ModeFor(CombatCommands.Command.Shoot),
            CombatCommandOutcome.ModeAfterCast(spellChosen: true));
    }

    [Fact]
    public void TheLabelAndTheCharacterScreenDoNeither() {
        // Neither touches the turn: the label is not clickable at all, and the character screen is a
        // suspend-and-return, not an action.
        Assert.False(CombatCommandOutcome.SpendsTheTurn(CombatCommands.Command.CapabilityLabel));
        Assert.False(CombatCommandOutcome.ArmsAPendingMode(CombatCommands.Command.CharacterScreen));
    }
}
