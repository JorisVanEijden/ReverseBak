namespace GameData.Resources.Data;

using System;

public class SaveGameContainerData {
    public SaveGameContainerData(
        SaveGameContainerLocationData location,
        SaveGameContainerType containerType,
        byte numberOfItems,
        byte capacity,
        SaveGameContainerDataType dataTypes,
        SaveGameInventoryItemData[] items,
        SaveGameContainerLockData? lockData,
        SaveGameContainerDialogData? dialogData,
        SaveGameContainerShopData? shopData,
        SaveGameContainerEncounterData? encounterData,
        int? timestamp,
        short? unknown20
    ) {
        Location = location;
        ContainerType = containerType;
        NumberOfItems = numberOfItems;
        Capacity = capacity;
        DataTypes = dataTypes;
        Items = items ?? Array.Empty<SaveGameInventoryItemData>();
        LockData = lockData;
        DialogData = dialogData;
        ShopData = shopData;
        EncounterData = encounterData;
        Timestamp = timestamp;
        Unknown20 = unknown20;
    }

    public SaveGameContainerLocationData Location { get; }
    public SaveGameContainerType ContainerType { get; }
    public byte NumberOfItems { get; }
    public byte Capacity { get; }
    public SaveGameContainerDataType DataTypes { get; }
    public SaveGameInventoryItemData[] Items { get; }
    public SaveGameContainerLockData? LockData { get; }
    public SaveGameContainerDialogData? DialogData { get; }
    public SaveGameContainerShopData? ShopData { get; }
    public SaveGameContainerEncounterData? EncounterData { get; }
    public int? Timestamp { get; }
    public short? Unknown20 { get; }

    public bool IsActorInventoryContainer {
        get => ContainerType == SaveGameContainerType.Inventory;
    }

    public short? OwnerActorNumber {
        get => IsActorInventoryContainer && Location.ActorNumber > 0
            ? Location.ActorNumber
            : null;
    }
}
