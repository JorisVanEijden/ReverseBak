namespace GameData.Resources.Inventory;

using GameData.Resources.Object;

/// <summary>
/// Putting an item into a container that did not come from another container — what a dialog does
/// when it hands the party something (<c>cmbinv_actor_acquire_item</c>).
///
/// <para><see cref="InventoryTransfer"/> deliberately only moves between containers, because every
/// other path in the game is a move. This is the one that creates.</para>
/// </summary>
public static class InventoryAcquire {
    /// <summary>
    /// Gives an item to a container if it will fit.
    /// </summary>
    /// <returns>
    /// False when there is no room — and the caller must act on that, because the original charges
    /// for the item only when it was actually accepted.
    /// </returns>
    /// <param name="sharedKeys">
    /// The party's one shared keys inventory. A KEY never reaches the member's pack:
    /// <c>cmbinv_actor_acquire_item</c> (CMBINV.C:1008) opens with
    /// <c>if (rec-&gt;wCategory == 7) { cmbinv_actor_pickup_item(...); return 1; }</c>, before the
    /// space test. Null disables the diversion, which is what a save with no such container gets.
    ///
    /// <para><b>This is not cosmetic.</b> The picklock screen builds its working set from the ring,
    /// so a key that landed in a pack is invisible to every lock. Measured 2026-09-10: Jimmy the
    /// Hand's Royal Key of Krondor — the ONLY copy of object 71 the game hands out, and the only
    /// thing that opens the chapter-1 palace grate — went into Owyn's pack and the grate's working
    /// set showed a Peasant's Key and two picklocks.</para>
    /// </param>
    /// <param name="receiverConditions">
    /// The named receiver's conditions, for the food arm below. Null skips it.
    /// </param>
    /// <param name="othersThenGround">
    /// The rest of the active party in roster order with their own conditions, and the ground pile
    /// LAST with a null for conditions.
    /// <b>The original does not give up when the named member is full.</b>
    /// <c>cmbinv_actor_acquire_item</c> (CMBINV.C:1022) falls out of the space test into a loop over
    /// <c>g_gameState.activeParty</c> and then a single attempt at <c>g_gameState.ground_pile</c>,
    /// returning 0 only when none of them had room. Passing nothing keeps the old single-container
    /// behaviour, which is what a caller with no party to hand should get.
    ///
    /// <para>Without it a dialog gift to a member with a full pack is <b>silently lost</b>: the
    /// caller reads the false as "not accepted" and correctly skips the charge, but the item is gone
    /// either way.</para>
    ///
    /// <para><b>Each entry carries its OWN conditions</b> because the original re-reads
    /// <c>actor-&gt;loc.party_slot.wSlot_id</c> inside the loop: the member who ends up holding the
    /// food is the one who eats it, not the one the gift was addressed to. The ground pile has no
    /// slot and no hunger, which is why its entry takes a null.</para>
    /// </param>
    public static bool TryGive(RuntimeContainer container, RuntimeItem item, ObjectInfoSet objects,
        RuntimeContainer sharedKeys = null,
        GameData.Resources.Character.ActorConditions receiverConditions = null,
        System.Collections.Generic.IReadOnlyList<(RuntimeContainer Container,
            GameData.Resources.Character.ActorConditions Conditions)> othersThenGround = null) {
        if (container == null || item == null) {
            return false;
        }
        if (sharedKeys != null && objects?.GetById(item.ObjectId)?.ObjectType == ObjectType.Key) {
            InventoryTransfer.AddKeyToRing(item, sharedKeys, objects);
            return true;
        }

        if (Place(container, item, objects)) {
            FeedIfStarving(container, item, objects, receiverConditions);
            return true;
        }

        // *** THE CASCADE. *** The others in roster order, then the ground.
        if (othersThenGround != null) {
            foreach ((RuntimeContainer fallback,
                GameData.Resources.Character.ActorConditions conditions) in othersThenGround) {
                if (fallback == null || ReferenceEquals(fallback, container)
                    || !Place(fallback, item, objects)) {
                    continue;
                }
                FeedIfStarving(fallback, item, objects, conditions);
                return true;
            }
        }

        return false;
    }

    /// <summary>Put the item in, with the tidy-up any acquisition gets.</summary>
    private static bool Place(RuntimeContainer container, RuntimeItem item, ObjectInfoSet objects) {
        if (!InventoryTransfer.CanFit(container, item, objects)) {
            return false;
        }

        container.Items.Add(item);
        container.Dirty = true;
        // The new item merges into an existing stack and the grid re-sorts, so a gift of arrows
        // lands on the stack already carried.
        InventoryOrder.Consolidate(container, objects,
            container.ContainerType == GameData.Resources.Data.SaveGameContainerType.Inventory);
        return true;
    }

    /// <summary>
    /// Being handed food while starving eats one immediately — CMBINV.C:1015-1019.
    /// </summary>
    /// <remarks>
    /// <b>This is the only recovery from Starving the game offers you in the field.</b> The original
    /// tests <c>rec-&gt;wCategory == 0x17</c> — category 23, <see cref="ObjectType.Food"/> — and then
    /// <c>abActorStatusRanks[partySlot][5]</c>, which is the <see cref="ActorCondition.Starving"/>
    /// rank, before calling <c>gstate_member_consume_rations(partySlot, 1)</c>. Measured in the
    /// Krondor sewers on 2026-09-10: with all three members at Starving 5, resting and camping heal
    /// nothing — which is faithful — so a party that runs out of food has no way back without this.
    ///
    /// <para><b>The rank is read by name, not as the literal 5</b>, which is what the shipped index
    /// happens to be; a literal here would silently follow the wrong condition if the order ever
    /// moved.</para>
    /// </remarks>
    private static void FeedIfStarving(RuntimeContainer container, RuntimeItem item,
        ObjectInfoSet objects, GameData.Resources.Character.ActorConditions conditions) {
        if (conditions == null || objects == null
            || objects.GetById(item.ObjectId)?.ObjectType != ObjectType.Food
            || !conditions.Has(ActorCondition.Starving)) {
            return;
        }

        GameData.Resources.Character.UpkeepEngine.ConsumeRations(
            container, conditions, objects.GetById);
    }
}
