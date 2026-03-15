namespace GameData.Resources.Data;

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

    public byte ShopType { get; }
    public byte MarkupPercentage { get; }
    public byte MaxHagglingDiscount { get; }
    public byte MarkDownPercentage { get; }
    public byte ShopkeeperSkill { get; }
    public byte TeleportParam { get; }
    public byte BardingDifficulty { get; }
    public byte BardingReward { get; }
    public byte BaseBardingReward { get; }
    public byte LastRestockChapter { get; }
    public byte InnRestHours { get; }
    public byte InnCostPerNight { get; }
    public byte RepairCategories { get; }
    public byte RepairCostMarkup { get; }
    public ShopItemCategories ShopCategories { get; }
}
