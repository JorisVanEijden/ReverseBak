namespace GameData.Resources.Inventory;

using GameData.Resources.Object;

/// <summary>
/// A carried light — the torch and the Ring of Prandur, the two <see cref="ObjectType.LightSource"/>
/// items: how long one burns, and what its timer running out does to it.
/// </summary>
public static class ItemLight {
    /// <summary>The light timer key a carried item uses: <see cref="World.LightSourceDecay.Source.Item"/>.</summary>
    public const int TimerKey = (int)World.LightSourceDecay.Source.Item;

    /// <summary>The Ring of Prandur — the other lit item the burn-down knows by id.</summary>
    public const int RingOfPrandurObjectId = 6;

    /// <summary>Game-time ticks in an hour of two-second ticks.</summary>
    public const long TicksPerHour = 0x708;

    /// <summary>
    /// How long one lighting burns: <c>palette_fade_schedule(0, wEffect_arg_a * 0x708)</c> — the
    /// record's first effect argument, in hours.
    /// </summary>
    public static long DurationTicks(ObjectInfo record) => (record?.EffectArgA ?? 0) * TicksPerHour;

    /// <summary>
    /// The carried light going out — <c>itemuse_party_tick_temporary</c> (ITEMUSE.C:593), run on the
    /// tick the item light timer reaches zero, once per party member's pack.
    /// </summary>
    /// <remarks>
    /// A lit torch is unlit and spends one torch, and the last one is discarded. A lit Ring of
    /// Prandur is unlit and spends a use, but keeps its slot at zero. Nothing else is touched, so
    /// an unlit stack beside the burning one is safe.
    /// </remarks>
    /// <returns>Whether anything went out.</returns>
    public static bool BurnDown(RuntimeContainer pack) {
        if (pack?.Items == null) {
            return false;
        }
        var changed = false;
        for (var i = 0; i < pack.Items.Count; i++) {
            RuntimeItem item = pack.Items[i];
            if ((item.ItemFlags & (ushort)ItemFlags.Lit) == 0) {
                continue;
            }
            if (item.ObjectId == InventoryConsume.TorchObjectId) {
                item.ItemFlags &= unchecked((ushort)~(ushort)ItemFlags.Lit);
                changed = true;
                if (item.Variable-- <= 1) {
                    pack.Items.RemoveAt(i);
                    i--;
                }
            } else if (item.ObjectId == RingOfPrandurObjectId) {
                item.ItemFlags &= unchecked((ushort)~(ushort)ItemFlags.Lit);
                changed = true;
                if (item.Variable != 0) {
                    item.Variable--;
                }
            }
        }
        if (changed) {
            pack.Dirty = true;
        }
        return changed;
    }
}
