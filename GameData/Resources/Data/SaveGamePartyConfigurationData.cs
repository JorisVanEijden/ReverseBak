namespace GameData.Resources.Data;

using System;

public class SaveGamePartyConfigurationData {
    public SaveGamePartyConfigurationData(
        byte numberOfActivePartyCharacters,
        byte[] activePartyCharacters,
        uint sharedInventoryPointer1,
        uint sharedInventoryPointer2,
        byte attributeIncreasedFlag,
        short rewardMoneyCounter,
        short[] initialAttributeGainModifiers,
        byte unusedPadding,
        SaveGameActorStatusEffectsData[] actorStatusEffects
    ) {
        NumberOfActivePartyCharacters = numberOfActivePartyCharacters;
        ActivePartyCharacters = activePartyCharacters ?? Array.Empty<byte>();
        SharedInventoryPointer1 = sharedInventoryPointer1;
        SharedInventoryPointer2 = sharedInventoryPointer2;
        AttributeIncreasedFlag = attributeIncreasedFlag;
        RewardMoneyCounter = rewardMoneyCounter;
        InitialAttributeGainModifiers = initialAttributeGainModifiers ?? Array.Empty<short>();
        UnusedPadding = unusedPadding;
        ActorStatusEffects = actorStatusEffects ?? Array.Empty<SaveGameActorStatusEffectsData>();
    }

    public byte NumberOfActivePartyCharacters { get; }
    public byte[] ActivePartyCharacters { get; }
    public uint SharedInventoryPointer1 { get; }
    public uint SharedInventoryPointer2 { get; }
    public byte AttributeIncreasedFlag { get; }
    public short RewardMoneyCounter { get; }
    public short[] InitialAttributeGainModifiers { get; }
    public byte UnusedPadding { get; }
    public SaveGameActorStatusEffectsData[] ActorStatusEffects { get; }
}
