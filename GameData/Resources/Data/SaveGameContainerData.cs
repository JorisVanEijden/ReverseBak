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
        short? globalStateIndex
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
        GlobalStateIndex = globalStateIndex;
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

    // Present when DataTypes has the (legacy-named) Unknown20 flag. For two-state world objects
    // (containerType 6, typeId 92<->93 e.g. lever/switch): sub_ovr192_20 (0x79830) reads
    // GetGlobalValue(GlobalStateIndex + 7000) as the object's on/off state and flips typeId
    // 92/93 accordingly. (IDA: containerData_unknown20.globalStateIndex.)
    public short? GlobalStateIndex { get; }

    public bool IsActorInventoryContainer {
        get => ContainerType == SaveGameContainerType.Inventory;
    }

    public short? OwnerActorNumber {
        get => IsActorInventoryContainer && Location.ActorNumber > 0
            ? Location.ActorNumber
            : null;
    }
}
