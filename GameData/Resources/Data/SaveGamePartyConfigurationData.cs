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
        SaveGameActorStatusEffectsData[] actorStatusEffects
    ) {
        NumberOfActivePartyCharacters = numberOfActivePartyCharacters;
        ActivePartyCharacters = activePartyCharacters ?? Array.Empty<byte>();
        SharedInventoryPointer1 = sharedInventoryPointer1;
        SharedInventoryPointer2 = sharedInventoryPointer2;
        AttributeIncreasedFlag = attributeIncreasedFlag;
        RewardMoneyCounter = rewardMoneyCounter;
        InitialAttributeGainModifiers = initialAttributeGainModifiers ?? Array.Empty<short>();
        ActorStatusEffects = actorStatusEffects ?? Array.Empty<SaveGameActorStatusEffectsData>();
    }

    public byte NumberOfActivePartyCharacters { get; }
    public byte[] ActivePartyCharacters { get; }
    public uint SharedInventoryPointer1 { get; }
    public uint SharedInventoryPointer2 { get; }
    public byte AttributeIncreasedFlag { get; }
    public short RewardMoneyCounter { get; }
    /// <summary>
    /// Six int16 — canassa's <c>aSkillTrainRate</c>. <b>Its purpose is not established</b>; the one
    /// routine that appeared to read it was a base+displacement into the next array.
    /// </summary>
    /// <remarks>
    /// It held TWO entries and a stray padding byte until 2026-08-24, which left the whole rest of
    /// this section — the condition ranks above all — seven bytes early. See TASK-203.
    /// </remarks>
    public short[] InitialAttributeGainModifiers { get; }
    public SaveGameActorStatusEffectsData[] ActorStatusEffects { get; }
}
