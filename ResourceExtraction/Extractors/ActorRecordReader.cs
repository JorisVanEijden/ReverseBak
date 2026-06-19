namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;
using System.IO;

/// <summary>
/// Shared reader for the fixed 95-byte BaK actor record — the per-character stat
/// block used both inside TEMP.GAM / save files (<see cref="SaveGameExtractor"/>)
/// and as the six initial-party templates in PARTY.DAT (<see cref="PartyExtractor"/>).
///
/// Layout (95 bytes):
///   +0x00 u16 namePointer            (in-file/runtime name pointer; 0 in PARTY.DAT)
///   +0x02 i16 knownSpells1..3        (spell bitmasks; 0 for non-casters)
///   +0x08 16 × AttributeValues(5)    (ActorAttribute order Health..Stealth)
///   +0x58 u8  actorNumber
///   +0x59 u32 inventoryPointer
///   +0x5D u16 combatDataPointer
///
/// Each attribute is {Maximum, Current, CurrentEffective, Experience, Modifier};
/// at game start Maximum == Current and the remaining three are 0.
/// </summary>
internal static class ActorRecordReader {
    /// <summary>Size of one actor record in bytes.</summary>
    public const int ActorStructSize = 95;

    public static SaveGameActorData ParseActor(BinaryReader reader) {
        return new SaveGameActorData(
            reader.ReadUInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            ParseAttributeValues(reader),
            reader.ReadByte(),
            reader.ReadUInt32(),
            reader.ReadUInt16()
        );
    }

    public static SaveGameAttributeValuesData ParseAttributeValues(BinaryReader reader) {
        return new SaveGameAttributeValuesData(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte()
        );
    }
}
