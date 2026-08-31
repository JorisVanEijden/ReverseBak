namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Clicking a ladder, tunnel or tunnel exit — <c>wcursor_click_fixedobj_picklock</c>.
/// </summary>
public class TraversalClickTests {
    [Fact]
    public void ItsDescribeLineIsNOTTheBuildings() {
        // Two fixed-object handlers, two describe records. Sharing one "you look at it" line
        // between them says the wrong thing on one of the two.
        Assert.NotEqual(FixedObjectClick.DescribeDialog, TraversalClick.DescribeDialog);
        Assert.Equal(0xae, TraversalClick.DescribeDialog);
    }

    [Fact]
    public void THELOCKFlowRunsWhateverTheLockValue() {
        // The building click tests lookupKey != 0 first; this one does not. picklock_screen_run is
        // called unconditionally and opens its prompt even for a key of zero, so there is no
        // "unlocked ladders work now" half to ship ahead of the screen.
        Assert.True(TraversalClick.LockFlowAlwaysRuns);
    }

    [Fact]
    public void ItEntersTheLockInItsOwnMode() {
        // Published as the dialog argument before the prompt plays, so a ladder can be worded
        // differently from a chest. The building uses 2.
        Assert.Equal(3, TraversalClick.LockMode);
    }

    [Fact]
    public void ASecondaryClickDescribesWhateverElseIsTrue() {
        Assert.Equal(TraversalClick.DescribeDialog,
            TraversalClick.DialogFor(isPrimary: false, hasFixedObject: true, lockOpened: true,
                interactDialogId: 1234));
    }

    [Fact]
    public void ASuccessfulLockPlaysTheObjectsOwnMessage() {
        // And THAT message is where the traversal lives — its Teleport action moves the party, so
        // the handler must not also move anyone.
        Assert.Equal(1234,
            TraversalClick.DialogFor(isPrimary: true, hasFixedObject: true, lockOpened: true,
                interactDialogId: 1234));
        Assert.True(TraversalClick.TraversalLivesInTheDialog);
    }

    [Fact]
    public void AMissingMessageSaysNothingHappensRatherThanFailingSilently() {
        Assert.Equal(TraversalClick.NothingToDoDialog,
            TraversalClick.DialogFor(isPrimary: true, hasFixedObject: true, lockOpened: true,
                interactDialogId: 0));
    }

    [Fact]
    public void ARefusedLockAddsNothingOfItsOwn() {
        // The lock flow has already had its say.
        Assert.Equal(0,
            TraversalClick.DialogFor(isPrimary: true, hasFixedObject: true, lockOpened: false,
                interactDialogId: 1234));
    }

    [Fact]
    public void NoFixedObjectThereIsNothingHappens() {
        Assert.Equal(TraversalClick.NothingToDoDialog,
            TraversalClick.DialogFor(isPrimary: true, hasFixedObject: false, lockOpened: true,
                interactDialogId: 1234));
    }

    [Fact]
    public void ItHasNoReachGuardWhereTheBuildingDoes() {
        // Copying the building's tile test here would make distant ladders silently unclickable.
        Assert.True(TraversalClick.HasNoReachGuard);
        Assert.False(FixedObjectClick.IsWithinReach(firesTrapEncounter: true, 4, 7, 5, 7));
    }
}
