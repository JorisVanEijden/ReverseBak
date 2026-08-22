namespace GameData.Resources.Combat;

using System.Collections.Generic;
using GameData;

/// <summary>
/// Finding a usable piece of equipment on a combatant — <c>cbstat_find_intact_equip_cat</c>
/// (canassa CBSTAT.C:415) and the condition rule behind it.
///
/// <para>Feeds the Shoot half of the HUD's capability cell (category
/// <see cref="CombatCapability.RangedWeaponCategory"/>) and the melee path's weapon lookup.</para>
/// </summary>
public static class EquippedGear {
    /// <summary>Condition given to an item whose type does not track wear.</summary>
    /// <remarks>
    /// The original writes the character literal <c>'d'</c> — 100 — so such an item is permanently
    /// "as new" rather than unusable. Reading it as 0 would make every simple weapon broken.
    /// </remarks>
    public const int UntrackedCondition = 100;

    /// <summary>
    /// The condition combat reads off an equipment slot.
    /// </summary>
    /// <param name="isBroken">The slot's Broken flag (<see cref="ItemFlags.Broken"/>, 0x10).</param>
    /// <param name="typeTracksCondition">
    /// Whether the item TYPE tracks wear — the item record's 0x1000 flag, a property of the kind of
    /// object, not of this particular one.
    /// </param>
    /// <param name="slotCondition">The slot's own condition, used only when the type tracks it.</param>
    /// <remarks>
    /// <b>Broken beats everything.</b> The flag is applied after the type test, so a broken item
    /// reads 0 even when its type does not track condition and would otherwise report
    /// <see cref="UntrackedCondition"/>.
    /// </remarks>
    public static int ConditionOf(bool isBroken, bool typeTracksCondition, int slotCondition) {
        if (isBroken) {
            return 0;
        }
        return typeTracksCondition ? slotCondition : UntrackedCondition;
    }

    /// <summary>
    /// <b>Asking for category 1 also accepts category 3 — and nothing else aliases.</b>
    /// </summary>
    /// <remarks>
    /// <c>altcategory = (category == 1) ? 3 : category</c>. So a melee-weapon lookup matches both
    /// kinds, while a ranged lookup (2) matches only 2. Applying the alias symmetrically would let a
    /// category-3 item answer a ranged query and offer a shot with the wrong weapon.
    /// </remarks>
    public static int AlternateCategoryFor(int category) => category == 1 ? 3 : category;

    /// <summary>Whether a slot satisfies a lookup for <paramref name="category"/>.</summary>
    /// <param name="equipped">The slot's Equipped flag (0x40) — unequipped gear is never found.</param>
    /// <param name="itemCategory">The item type's category.</param>
    /// <param name="condition">From <see cref="ConditionOf"/>.</param>
    /// <remarks>
    /// <b>Intact means condition strictly above zero</b>, not above some usable floor: a weapon worn
    /// down to 1 still counts. The only way to fail is to be broken, or to be an item whose type
    /// tracks condition and has reached 0.
    /// </remarks>
    public static bool SlotSatisfies(bool equipped, int itemCategory, int condition, int category) =>
        equipped
        && (itemCategory == category || itemCategory == AlternateCategoryFor(category))
        && condition > 0;

    /// <summary>An equipment slot as this lookup sees it.</summary>
    public readonly struct Slot {
        public Slot(bool equipped, int category, int condition) {
            Equipped = equipped;
            Category = category;
            Condition = condition;
        }

        public bool Equipped { get; }
        public int Category { get; }

        /// <summary>Already resolved through <see cref="ConditionOf"/>.</summary>
        public int Condition { get; }
    }

    /// <summary>
    /// Whether the combatant has an intact, equipped item of this category.
    /// </summary>
    /// <remarks>
    /// The original returns the item; callers only ever test it against null, so this answers the
    /// question they actually ask. <b>First match wins</b> in slot order, which matters only if a
    /// caller later needs the item itself.
    /// </remarks>
    public static bool HasIntact(IEnumerable<Slot> slots, int category) {
        if (slots == null) {
            return false;
        }
        foreach (Slot slot in slots) {
            if (SlotSatisfies(slot.Equipped, slot.Category, slot.Condition, category)) {
                return true;
            }
        }
        return false;
    }
}
