namespace GameData.Resources.Object;

using GameData;

public class ObjectInfo : IResource {
    public ObjectInfo(string id) {
        Id = id;
    }

    public string Name { get; set; }

    /// <summary>+0x1E. <b>Editor-side melee-weapon/armor group marker — no runtime reader.</b>
    /// Ships exactly two values: <c>0x1F80</c> on the 17 <c>Sword</c>+<c>Armor</c> items, and
    /// <c>0</c> on all 121 others. The set is precisely <c>ObjectType ∈ {Sword, Armor}</c> —
    /// note it <i>excludes</i> Crossbows (objects 30–35), which degrade yet carry 0 here, so it is
    /// NOT a "degradable equipment" flag. A field-level xref sweep finds no code reading +0x1E
    /// (the 80-byte <c>objectInfo</c> struct is indexed widely but nothing dereferences this
    /// offset). Redundant with <see cref="ObjectType"/>; vestigial at runtime. Preserved verbatim.
    /// Verified 2026-06-26.</summary>
    public int Field1E { get; set; }

    public ObjectFlags Flags { get; set; }
    public int WordWrap { get; set; }
    public int ChapterNumber { get; set; }
    public int Price { get; set; }
    public int SwingBaseDamage { get; set; }
    public int ThrustBaseDamage { get; set; }
    public int SwingAccuracy_ArmorMod_BowAccuracy { get; set; }
    public int ThrustAccuracy { get; set; }

    /// <summary>+0x30. Inventory-icon override. <b>0 is a valid sentinel, not missing data</b>:
    /// getIconImageData (IDA 0x56185) computes the INVSHP image index as
    /// <c>icon != 0 ? icon : objectNumber</c> — so 0 means "use this object's own number as the
    /// icon index" (the identity default, which is why ~121/138 objects ship 0). Indices &lt; 120
    /// select from INVSHP1.BMX, &gt;= 120 from INVSHP2.BMX (index-120). A few items override at
    /// runtime (lit torch → INVSHP2[8], broken crossbow, lit Ring of Prandur). Verified 2026-06-02.</summary>
    public int Icon { get; set; }

    public int InventorySlots { get; set; }
    public int SoundId { get; set; }
    public int SoundRepeat { get; set; }
    public int MaxAmount { get; set; }

    /// <summary>+0x37. Maximum charges/uses of a limited-use item (the denominator that
    /// prorates resale value). Reversed from the item-value calc <c>sub_ovr159_109</c>
    /// (0x578a9): for items flagged <c>LimitedUses</c>/<c>0x8000</c> the value is
    /// <c>basePrice × remainingCharges / MaxCharges</c>. Non-zero only on charge-bearing
    /// items — staves (40/50), combat items (6–10), quarrel stacks (25), money (255/100).</summary>
    public int MaxCharges { get; set; }

    public Race Race { get; set; }
    public int ShopType { get; set; }
    public ObjectType ObjectType { get; set; }

    /// <summary>+0x3E. <b>The first word of the four-word effect-parameter block, and it means a
    /// different thing per <see cref="ObjectType"/></b> — <c>Use_Item</c> @0x58cbd reads it seven
    /// ways (canassa's <c>wEffect_arg_a</c>, ITEMUSE.C):
    /// <list type="table">
    /// <item><term>8 repair kit</term><description>the target <b>category number</b> (1 Sword,
    /// 2 Crossbow, 4 Armor), which also picks the skill: 4 → ArmorCraft, else WeaponCraft.</description></item>
    /// <item><term>9 / 10 / 11 coatings</term><description>an <see cref="ItemFlags"/> <b>SET</b>
    /// mask, applied with <see cref="EffectArgB"/> as the keep mask.</description></item>
    /// <item><term>17 stat effect</term><description>an <b>actor-attribute</b> mask — the one
    /// reading the old <c>ActorAttributeFlag</c> typing was right about
    /// (<c>itemuse_apply_stat_effects</c>).</description></item>
    /// <item><term>18 timed modifier</term><description>the modifier record's <b>flags word</b>:
    /// 0x100 combat-only, 0x200 timed (expires after <see cref="EffectDurationHours"/>),
    /// 0x400/0x800 the amount is a percentage rather than a flat add (<c>sub_ovr132_0</c>
    /// 0x42ea0).</description></item>
    /// <item><term>19 food</term><description>the <b>heal amount</b>.</description></item>
    /// <item><term>20 direct delta</term><description>a <b>stat index</b>.</description></item>
    /// <item><term>21 torch</term><description>the <b>burn duration in hours</b>
    /// (<c>arg_a × 0x708</c> ticks).</description></item>
    /// </list>
    /// Emitted raw for that reason: any single decode would be a lie for six of the seven. The
    /// per-category dispatch that reads it is <c>GameData.Resources.Inventory.InventoryUse</c>;
    /// see docs/specs/inventory-item-handling.md §17.2. (Was <c>Attributes</c>, typed
    /// <see cref="ActorAttributeFlag"/> — task-77.)</summary>
    public int EffectArgA { get; set; }

    /// <summary>+0x40. The second word of the effect-parameter block, equally per-category
    /// (canassa's <c>wEffect_arg_b</c>): the <see cref="ItemFlags"/> <b>keep</b> mask for the
    /// coating categories (0xE07F = "keep everything but the other coatings"; Coltari Poison's 0
    /// wipes the lot), the <b>affected-attribute mask</b> of a timed modifier (category 18, the
    /// reading its old name <c>UseEffectAttributeMask</c> described), the effect <b>amount</b>
    /// (&lt;&lt; 8) for category 17, the random <b>heal range</b> for food, the <b>delta</b> for
    /// category 20. Unused by repair kits and torches.</summary>
    public int EffectArgB { get; set; }

    /// <summary>+0x42. <b>Use/timed-effect magnitude</b> (canassa's <c>wEffect_chance_pct</c>):
    /// the modifier's value for category 18 — a flat add, or a percentage when
    /// <see cref="EffectArgA"/> carries 0x400/0x800 — and the re-use chance percent for
    /// category 17. Non-zero on potions/books that grant a graded buff.</summary>
    public int UseEffectAmount { get; set; }

    /// <summary>+0x44. <b>Timed-effect duration in game hours.</b> <c>Use_Item</c> sets the
    /// active effect's expiry to <c>GameTimeIn2Seconds + EffectDurationHours × 0x708</c> (one
    /// hour = 0x708 two-second ticks). Category 17 instead reads it as the amount applied on a
    /// repeat use. (Prior name "Book1Potion8" was a guess.)</summary>
    public int EffectDurationHours { get; set; }

    /// <summary>+0x46. <b>Equipped/carried passive-modifier attribute bitmask.</b> Read as an
    /// unsigned 16-bit mask by <c>ApplyAllModifiersFromItemsInInventory</c> (0x42f02): for each
    /// attribute <c>n</c> with <c>(mask &amp; (1 &lt;&lt; n)) != 0</c>, the actor's attribute
    /// modifier gets <see cref="EquipModifierAmount"/> added. This is the passive worn-item
    /// bonus path (rings, amulets, Weedwalkers), distinct from the use-effect path above.
    ///
    /// <para>The one field in the record that is unconditionally <c>1 &lt;&lt; ActorAttribute</c>,
    /// so it carries the <see cref="ActorAttributeFlag"/> typing that used to sit on
    /// <see cref="EffectArgA"/> — a ring now reads <c>"Scouting, Stealth"</c> instead of 0xC000
    /// (task-77). (Prior name "CanEffect".)</para></summary>
    public ActorAttributeFlag EquipAttributeMask { get; set; }

    /// <summary>+0x48. <b>Equipped/carried passive-modifier amount</b> (signed) added to each
    /// attribute selected by <see cref="EquipAttributeMask"/>. Can be negative — e.g. Idol of
    /// Lassur ships −20 across many attributes. Applied as a signed byte by
    /// <c>ApplyAllModifiersFromItemsInInventory</c>.</summary>
    public int EquipModifierAmount { get; set; }

    /// <summary>+0x4A. <b>Per-event degradation chance, percent (0–99).</b>
    /// <c>ApplyItemDegradation</c> (0x6bceb) degrades an equipped degradable item when a random
    /// 0–99 roll is below this value.</summary>
    public int DegradeChancePercent { get; set; }

    /// <summary>+0x4C. <b>Maximum wear removed per degradation event.</b> When an item degrades,
    /// <c>ApplyItemDegradation</c> subtracts a random 1..MaxWearPerDegrade (scaled by the
    /// caller's degrade percentage) from the item's quality.</summary>
    public int MaxWearPerDegrade { get; set; }

    /// <summary>+0x4E. <b>Minimum quality floor.</b> Degradation never drops an item's quality
    /// below this value (<c>ApplyItemDegradation</c> clamps the new quality up to it); at quality
    /// ≤ 0 the item is flagged broken.</summary>
    public int MinimumQuality { get; set; }

    public int Number { get; set; }
    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }

    public string ToCsv() {
        return
            $"{Number},{Name},{Field1E},{ToBooleans(Flags)},{WordWrap},{ChapterNumber},{Price},{SwingBaseDamage},{ThrustBaseDamage},{SwingAccuracy_ArmorMod_BowAccuracy},{ThrustAccuracy},{Icon},{InventorySlots},{SoundId},{MaxAmount},{MaxCharges},{Race},{ShopType:X4},{ObjectType},{EffectArgA:X4},{EffectArgB:X4},{UseEffectAmount},{EffectDurationHours},\"{EquipAttributeMask}\",{EquipModifierAmount},{DegradeChancePercent},{MaxWearPerDegrade},{MinimumQuality}";
    }

    private static string ToBooleans(ObjectFlags flags) {
        char[] bits = new char[16];
        for (int i = 15; i >= 0; i--) {
            if (((int)flags & 1 << i) != 0) {
                bits[i] = '#';
            } else {
                bits[i] = '.';
            }
        }
        return string.Join(',', bits);
    }
}