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

    /// <summary>
    /// The owning actor's number, as stored — <b>1-based</b>, so Locklear is 1 and 0 means "no
    /// actor". Compare it against <c>SaveGameActorData.ActorNumber</c>, which is stored the same
    /// way; to index the party record set use <see cref="OwnerPartyPosition"/> instead.
    /// </summary>
    public short? OwnerActorNumber {
        get => IsActorInventoryContainer && Location.ActorNumber > 0
            ? Location.ActorNumber
            : null;
    }

    /// <summary>
    /// The owning member's index in the party record set, or null when the container is not a
    /// member's pack.
    /// </summary>
    /// <remarks>
    /// <b>The stored actor number is one MORE than the record index.</b> Confirmed from the
    /// disassembly rather than inferred: <c>canAutoEquipOnPickup</c> @0x55414 turns the field into
    /// an actor pointer with <c>actorNr * 0x5F + 0x3E56</c>, and <c>actors_Locklear</c> sits at
    /// dseg offset <c>0x3EB5</c> — exactly one 0x5F-byte record ABOVE that base. So the expression
    /// is <c>actors_Locklear[actorNr - 1]</c>, and the base is deliberately biased down by one.
    ///
    /// <para>The two numberings coexist in the same engine: <c>activePartyCharacters</c> is 0-based
    /// and indexes from <c>actors_Locklear</c> itself (<c>UI_Encamp</c> @0x7066e), while this field
    /// is 1-based. Treating this one as 0-based hands every member the pack belonging to the member
    /// before them and leaves Locklear — actor 1, and the only one with no lower neighbour — with
    /// no pack at all.</para>
    /// </remarks>
    public int? OwnerPartyPosition => OwnerActorNumber - 1;
}
