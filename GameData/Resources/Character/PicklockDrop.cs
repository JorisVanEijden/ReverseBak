namespace GameData.Resources.Character;

/// <summary>
/// What dropping something on the lock does, and what has to be written back —
/// <c>sub_ovr166_210</c> @0x5beb0.
/// </summary>
/// <remarks>
/// <see cref="PicklockAttempt"/> already carries the arithmetic (whether it opens, whether the tool
/// snaps, what skill is awarded). This is the surrounding flow: which dialog is shown, and — the
/// part that is easy to get wrong — WHERE a broken tool has to be removed from.
/// </remarks>
public static class PicklockDrop {
    /// <summary>A key's object id, from the lock number it fits.</summary>
    /// <remarks>The original adds 60 to the lock number (0x5bf8d).</remarks>
    public const int KeyObjectIdBase = 60;

    /// <summary>The object id of the key that opens a given lock.</summary>
    public static int KeyObjectIdFor(int lockNumber) => KeyObjectIdBase + lockNumber;

    /// <summary>"The key works" — the lock opens.</summary>
    public const int KeyWorkedDialog = 81;

    /// <summary>"Wrong key" — it did not fit and did not break.</summary>
    public const int WrongKeyDialog = 82;

    /// <summary>"The key broke".</summary>
    public const int KeyBrokeDialog = 245;

    /// <summary>"The picklock works" — the lock opens.</summary>
    public const int PicklockWorkedDialog = 83;

    /// <summary>"You cannot open it" — the pick failed and survived.</summary>
    public const int CannotOpenDialog = 84;

    /// <summary>"The picklock broke".</summary>
    public const int PicklockBrokeDialog = 85;

    /// <summary>Which dialog a completed attempt shows.</summary>
    public static int DialogFor(bool usedPicklocks, PicklockAttempt.AttemptResult result) =>
        usedPicklocks
            ? result switch {
                PicklockAttempt.AttemptResult.Opened => PicklockWorkedDialog,
                PicklockAttempt.AttemptResult.ToolBroke => PicklockBrokeDialog,
                _ => CannotOpenDialog,
            }
            : result switch {
                PicklockAttempt.AttemptResult.Opened => KeyWorkedDialog,
                PicklockAttempt.AttemptResult.ToolBroke => KeyBrokeDialog,
                _ => WrongKeyDialog,
            };

    /// <summary>
    /// Where a snapped tool has to be taken from, beyond the working set itself.
    /// </summary>
    /// <remarks>
    /// <b>The two tools are removed from different places, and this is the whole reason the
    /// distinction matters.</b> Both are first removed from the scratch container the screen is
    /// showing; what differs is the second step:
    ///
    /// <list type="bullet">
    /// <item><b>A key</b> is removed from the party's SHARED inventory explicitly — the original
    /// calls the same removal twice, once per container (0x5bf96, 0x5bfa9), because that is where
    /// keys actually live.</item>
    /// <item><b>A picklock</b> goes through the generic consume-one-from-the-party path instead
    /// (<c>useItem</c> at 0x5c093). The displayed pick stack is a SYNTHETIC aggregate with no
    /// owning member — see <see cref="PicklockWorkingSet"/> — so there is no container to remove it
    /// from, and deciding which member loses it is not this code's job.</item>
    /// </list>
    ///
    /// <para>Implementing both as "remove from the shared inventory" loses picks that are not there;
    /// implementing both as "consume from the party" would let a key be taken from a member's pack,
    /// where keys never are.</para>
    /// </remarks>
    public enum BreakageTarget {
        /// <summary>Nothing broke.</summary>
        None,

        /// <summary>Remove from the working set and from the party's shared inventory.</summary>
        SharedInventory,

        /// <summary>Remove from the working set, then consume one from the party at large.</summary>
        PartyAtLarge,
    }

    /// <summary>Where the breakage has to be applied, given what was dropped and what happened.</summary>
    public static BreakageTarget BreakageFor(bool usedPicklocks,
        PicklockAttempt.AttemptResult result) =>
        result != PicklockAttempt.AttemptResult.ToolBroke
            ? BreakageTarget.None
            : usedPicklocks
                ? BreakageTarget.PartyAtLarge
                : BreakageTarget.SharedInventory;

    /// <summary>
    /// Destroys the tool that just snapped, in both the places it has to come out of.
    /// </summary>
    /// <param name="droppedObjectId">
    /// <b>The object actually dropped on the lock</b>, which the caller must carry through from the
    /// drop. It cannot be recovered afterwards by looking at the working set: the party can hold
    /// several keys at once, so "the key in there" is not a question with one answer, and answering
    /// it by picking the first one destroys a key the player never touched.
    /// </param>
    /// <param name="workingSet">The screen's scratch container — always loses one.</param>
    /// <param name="sharedInventory">
    /// The party's shared stock. Loses the key as well; ignored for a picklock, which was never in
    /// it.
    /// </param>
    /// <param name="partyPacks">
    /// Each member's pack in roster order, walked only for a picklock. The first pack holding one
    /// loses it — the engine's own consume-by-kind walk, and the reason the synthetic pick stack
    /// does not need an owner.
    /// </param>
    /// <param name="lookup">Object id to record; pass <c>objectInfoSet.GetById</c>.</param>
    /// <returns>Whether anything was destroyed — false when nothing broke.</returns>
    public static bool ApplyBreakage(bool usedPicklocks, PicklockAttempt.AttemptResult result,
        int droppedObjectId, Inventory.RuntimeContainer workingSet,
        Inventory.RuntimeContainer sharedInventory,
        System.Collections.Generic.IEnumerable<Inventory.RuntimeContainer> partyPacks,
        System.Func<int, Object.ObjectInfo> lookup) {
        BreakageTarget target = BreakageFor(usedPicklocks, result);
        if (target == BreakageTarget.None) {
            return false;
        }

        if (workingSet != null) {
            Inventory.InventoryConsume.TryConsumeOne(workingSet, droppedObjectId, lookup);
        }

        if (target == BreakageTarget.SharedInventory) {
            return sharedInventory != null
                && Inventory.InventoryConsume.TryConsumeOne(sharedInventory, droppedObjectId, lookup);
        }

        foreach (Inventory.RuntimeContainer pack in partyPacks
            ?? System.Array.Empty<Inventory.RuntimeContainer>()) {
            if (pack != null
                && Inventory.InventoryConsume.TryConsumeOne(pack, droppedObjectId, lookup)) {
                return true;
            }
        }

        return false;
    }
}
