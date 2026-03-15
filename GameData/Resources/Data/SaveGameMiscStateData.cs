namespace GameData.Resources.Data;

public class SaveGameMiscStateData {
    public SaveGameMiscStateData(
        short dialogPrimaryActorNumber,
        short dialogSecondaryActorNumber,
        short dialogTertiaryActorNumber,
        short unusedPadding,
        short global30000,
        short creatureType,
        short keyObjectId,
        short global30013AttributeValue,
        int global30014Money,
        int global30015,
        int global30018
    ) {
        DialogPrimaryActorNumber = dialogPrimaryActorNumber;
        DialogSecondaryActorNumber = dialogSecondaryActorNumber;
        DialogTertiaryActorNumber = dialogTertiaryActorNumber;
        UnusedPadding = unusedPadding;
        Global30000 = global30000;
        CreatureType = creatureType;
        KeyObjectId = keyObjectId;
        Global30013AttributeValue = global30013AttributeValue;
        Global30014Money = global30014Money;
        Global30015 = global30015;
        Global30018 = global30018;
    }

    public short DialogPrimaryActorNumber { get; }
    public short DialogSecondaryActorNumber { get; }
    public short DialogTertiaryActorNumber { get; }
    public short UnusedPadding { get; }
    public short Global30000 { get; }
    public short CreatureType { get; }
    public short KeyObjectId { get; }
    public short Global30013AttributeValue { get; }
    public int Global30014Money { get; }
    public int Global30015 { get; }
    public int Global30018 { get; }
}
