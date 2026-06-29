namespace GameData.Resources.Data;

/// <summary>
/// The container's 16-byte "Shop" data block — a faithful 1:1 mirror of the binary.
/// </summary>
/// <remarks>
/// This block is a <b>type-discriminated union</b>: the same container struct backs shops, taverns,
/// inns, repair stalls and temple-teleport nodes (a container can offer several at once). Which fields
/// are meaningful — and how the three <b>overloaded</b> bytes below are interpreted — depends on the
/// container type, which is decided by the GDS scene's hotspot ActionCode, not by this block:
/// <list type="bullet">
///   <item><see cref="ShopType"/> (+0): shop variant id <i>or</i> temple node index (1..12).</item>
///   <item><see cref="TeleportParam"/> (+5): teleport cost-per-distance <i>or</i> the shopkeeper's
///   refuse-to-sell chance during haggling.</item>
///   <item><see cref="ShopCategories"/> (+0xE): traded item-category flags <i>or</i> base teleport cost.</item>
/// </list>
/// A semantic, modder-friendly reshape (composable <c>Establishment</c> services) is designed but
/// deferred until establishments enter gameplay — see
/// <c>docs/superpowers/specs/2026-06-28-establishment-semantic-shape-design.md</c> and
/// <c>docs/shop-pricing.md</c>.
/// </remarks>
public class SaveGameContainerShopData {
    public SaveGameContainerShopData(
        byte shopType,
        byte markupPercentage,
        byte maxHagglingDiscount,
        byte markDownPercentage,
        byte shopkeeperSkill,
        byte teleportParam,
        byte bardingDifficulty,
        byte bardingReward,
        byte baseBardingReward,
        byte lastRestockChapter,
        byte innRestHours,
        byte innCostPerNight,
        byte repairCategories,
        byte repairCostMarkup,
        ShopItemCategories shopCategories
    ) {
        ShopType = shopType;
        MarkupPercentage = markupPercentage;
        MaxHagglingDiscount = maxHagglingDiscount;
        MarkDownPercentage = markDownPercentage;
        ShopkeeperSkill = shopkeeperSkill;
        TeleportParam = teleportParam;
        BardingDifficulty = bardingDifficulty;
        BardingReward = bardingReward;
        BaseBardingReward = baseBardingReward;
        LastRestockChapter = lastRestockChapter;
        InnRestHours = innRestHours;
        InnCostPerNight = innCostPerNight;
        RepairCategories = repairCategories;
        RepairCostMarkup = repairCostMarkup;
        ShopCategories = shopCategories;
    }

    /// <summary>+0. Overloaded: shop variant id (used only by the zone-3 "6× currency" price quirk)
    /// <b>or</b> the temple's teleport node index (1..12).</summary>
    public byte ShopType { get; }
    public byte MarkupPercentage { get; }
    public byte MaxHagglingDiscount { get; }
    public byte MarkDownPercentage { get; }
    public byte ShopkeeperSkill { get; }
    /// <summary>+5. Overloaded: temple teleport cost-per-distance <b>or</b> (for shops) the shopkeeper's
    /// chance to refuse the sale on a failed haggle.</summary>
    public byte TeleportParam { get; }
    public byte BardingDifficulty { get; }
    public byte BardingReward { get; }
    public byte BaseBardingReward { get; }
    public byte LastRestockChapter { get; }
    public byte InnRestHours { get; }
    public byte InnCostPerNight { get; }
    public byte RepairCategories { get; }
    public byte RepairCostMarkup { get; }
    /// <summary>+0xE. Overloaded: traded item-category flags (shops) <b>or</b> the base teleport cost
    /// (temples).</summary>
    public ShopItemCategories ShopCategories { get; }
}
