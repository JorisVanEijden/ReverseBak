namespace GameData.Resources.Object;

using GameData;

public class ObjectInfo : IResource {
    public ObjectInfo(string id) {
        Id = id;
    }

    public string Name { get; set; }

    /// <summary>+0x1E. <b>Unknown — no reader found.</b> The 80-byte <c>objectInfo</c> struct is
    /// indexed all over the engine, but a field-level xref sweep finds no code reading +0x1E.
    /// Only two values ship: 0 (121 items) and 0x1F80 (17 items), so it is likely a vestigial
    /// editor flag. Preserved verbatim.</summary>
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

    /// <summary>+0x3E. <b>Use/timed-effect flags + (for some non-consumables) a skill/attribute
    /// target.</b> Copied to the active-effect record's flags word by <c>Use_Item</c> and read
    /// by <c>sub_ovr132_0</c> (0x42ea0): bit <c>0x100</c> = combat-only, <c>0x200</c> = timed
    /// (expires after <see cref="EffectDurationHours"/>), <c>0x400</c>/<c>0x800</c> = the effect
    /// is a <i>percentage</i> modifier (<c>value × (amount+100)/100</c>) rather than a flat add.
    /// Must be non-zero for an active effect to apply (<c>GetAttributeFromActor</c> guard).
    /// The low bits additionally encode the affected skill on repair/enhancer tools (e.g.
    /// Whetstone 0x1, Armorer's Hammer 0x4) — hence the historical <c>ActorAttributeFlag</c>
    /// decode, which is meaningful for those items but NOT for the high flag bits above.</summary>
    public ActorAttributeFlag Attributes { get; set; }

    /// <summary>+0x40. <b>Use/timed-effect affected-attribute bitmask.</b> For an item consumed
    /// via <c>Use_Item</c>, this selects which actor attributes the effect modifies:
    /// <c>GetAttributeFromActor</c> (0x42fca) applies the effect to attribute <c>n</c> iff
    /// <c>(mask &amp; (1 &lt;&lt; n)) != 0</c>. Single-bit for most potions (e.g. Truesight Tea
    /// 0x20); many bits for broad coatings (0xE07F). (Prior RE called this "effectStrength" —
    /// that name is wrong; the magnitude is <see cref="UseEffectAmount"/>.)</summary>
    public int UseEffectAttributeMask { get; set; }

    /// <summary>+0x42. <b>Use/timed-effect magnitude.</b> The amount applied to each attribute
    /// selected by <see cref="UseEffectAttributeMask"/>, read by <c>sub_ovr132_0</c>: a flat
    /// add, or — when <see cref="Attributes"/> has bit 0x400/0x800 — a percentage
    /// (<c>value × (amount+100)/100</c>). Non-zero on potions/books that grant a graded buff.</summary>
    public int UseEffectAmount { get; set; }

    /// <summary>+0x44. <b>Timed-effect duration in game hours.</b> <c>Use_Item</c> sets the
    /// active effect's expiry to <c>GameTimeIn2Seconds + EffectDurationHours × 0x708</c> (one
    /// hour = 0x708 two-second ticks). (Prior name "Book1Potion8" was a guess; the loader uses
    /// it purely as the effect duration.)</summary>
    public int EffectDurationHours { get; set; }

    /// <summary>+0x46. <b>Equipped/carried passive-modifier attribute bitmask.</b> Read as an
    /// unsigned 16-bit mask by <c>ApplyAllModifiersFromItemsInInventory</c> (0x42f02): for each
    /// attribute <c>n</c> with <c>(mask &amp; (1 &lt;&lt; n)) != 0</c>, the actor's attribute
    /// modifier gets <see cref="EquipModifierAmount"/> added. This is the passive worn-item
    /// bonus path (rings, amulets, Weedwalkers), distinct from the use-effect path above.
    /// (Prior name "CanEffect".)</summary>
    public int EquipAttributeMask { get; set; }

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
            $"{Number},{Name},{Field1E},{ToBooleans(Flags)},{WordWrap},{ChapterNumber},{Price},{SwingBaseDamage},{ThrustBaseDamage},{SwingAccuracy_ArmorMod_BowAccuracy},{ThrustAccuracy},{Icon},{InventorySlots},{SoundId},{MaxAmount},{MaxCharges},{Race},{ShopType:X4},{ObjectType},\"{Attributes}\",{UseEffectAttributeMask:X4},{UseEffectAmount},{EffectDurationHours},{EquipAttributeMask:X4},{EquipModifierAmount},{DegradeChancePercent},{MaxWearPerDegrade},{MinimumQuality}";
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