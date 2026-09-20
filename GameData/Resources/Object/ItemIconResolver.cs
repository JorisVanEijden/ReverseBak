namespace GameData.Resources.Object;

using GameData.Resources.Inventory;

/// <summary>
/// Faithful port of getIconImageData (KRONDOR.EXE 0x56185): an item's INVSHP bitmap.
/// index = Icon != 0 ? Icon : Number;  index &lt; 120 -> INVSHP1.BMX#index,  else INVSHP2.BMX#(index-120).
/// </summary>
public static class ItemIconResolver {
    /// <summary>The first index that lives in INVSHP2 rather than INVSHP1.</summary>
    public const int SecondSheetBase = 120;

    /// <summary>A lit torch's own icon — the Hi table's entry 8, so INVSHP2.BMX#8.</summary>
    /// <remarks>
    /// <c>invui_item_sprite_select</c> (canassa INVENTOR.C:117) replaces the sprite outright:
    /// <c>if (slot-&gt;item_id == 0x54 &amp;&amp; (slot-&gt;flags &amp; 1)) pSprite =
    /// g_pInvSpriteHiAssetTable[8];</c>. The Hi table IS INVSHP2, so this is index 8 of that sheet
    /// and NOT 120 + 8.
    /// </remarks>
    public const int LitTorchIcon = 8;

    /// <summary>The item's icon, ignoring anything only a carried stack can know.</summary>
    public static string ResolveBmxSubResource(ObjectInfo obj) => ResolveBmxSubResource(obj, 0);

    /// <summary>
    /// The item's icon, including the overrides a LIT item gets.
    /// </summary>
    /// <remarks>
    /// <b>Only the torch changes sprite.</b> <c>invui_item_sprite_select</c> has three runtime
    /// arms and they are not the same kind of thing:
    /// <list type="bullet">
    /// <item>a lit <b>torch</b> (0x54) is given a different bitmap — this one;</item>
    /// <item>a lit <b>Ring of Prandur</b> (6) keeps its bitmap and shifts eight palette entries
    /// (<c>LUT[0x88..0x8f] -= 4</c>), so it glows rather than becoming another picture;</item>
    /// <item>a category-2 item flagged 0x10 zeroes <c>LUT[0xca]</c>, again a palette change.</item>
    /// </list>
    /// So a port that treats "lit" as one rule gets the ring wrong. Only the torch belongs here;
    /// the other two are the renderer's palette business and are deliberately not faked with a
    /// second sprite.
    /// </remarks>
    /// <param name="flags">The carried item's flags — <see cref="ItemFlags.Lit"/> is bit 0.</param>
    public static string ResolveBmxSubResource(ObjectInfo obj, ushort flags) {
        if (obj == null) {
            return null;
        }

        bool lit = (flags & (ushort)ItemFlags.Lit) != 0;
        if (lit && obj.Number == InventoryConsume.TorchObjectId) {
            return $"INVSHP2.BMX#{LitTorchIcon}";
        }

        int index = obj.Icon != 0 ? obj.Icon : obj.Number;
        return index < SecondSheetBase
            ? $"INVSHP1.BMX#{index}"
            : $"INVSHP2.BMX#{index - SecondSheetBase}";
    }
}
