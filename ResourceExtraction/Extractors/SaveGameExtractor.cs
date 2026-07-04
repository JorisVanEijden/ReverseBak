namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;
using GameData.Resources.Dialog.Actions;
using GameData.Resources.Location;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class SaveGameExtractor : ExtractorBase<SaveGame> {
    private const int StateDataSize = 2775;
    private const int WorldDataSize = 34320;
    private const int ActorDataSize = 164350;
    private const int CombatDataSize = 38060;
    private const int ZoneContainerDataSize = 95000;
    private const int ZoneContainerSectionStartOffset = StateDataSize + WorldDataSize + ActorDataSize + CombatDataSize;
    private const int TempGameDataSize = StateDataSize + WorldDataSize + ActorDataSize + CombatDataSize + ZoneContainerDataSize;
    private const int PartyActorCount = 6;
    private const int TimedEffectsPerActor = 8;
    private const int ActorNameLength = 10;
    private const int ActorStructSize = ActorRecordReader.ActorStructSize;
    private const int ActorStatusEffectsSize = 7;
    private const int TimedEffectSize = 14;
    private const int TimerCount = 20;
    private const int TimerSize = 8;
    private const int GlobalFlagsSize = 1063;
    private const int GlobalFlags2Size = 50;
    private const int ExplorationStateEntryCount = 40;
    private const int ExploredTileBitfieldSize = 38;
    private const int ZoneDataEntryCount = 20;
    private const int ChapterGoldEntryCount = 10;
    private const int EnemyPartyRecordCount = 700;
    private const int EnemyPartySlotsPerRecord = 7;
    private const int WorldEncounterZoneCount = 40;
    private const int WorldEncounterItemsPerZone = 35;
    private const int ActorSlotCount = 1730;
    private const int CombatSlotCount = 1730;
    private const int PartyActorDataSize = ActorStructSize * PartyActorCount;

    // FULLMAP.SCX reference resolution; the header's worldX/worldY are pixels on it.
    private const float FullMapWidth = 320f;
    private const float FullMapHeight = 200f;
    // The load path (StartGameOrLoadSave 0x208fc) adds 2 to the stored icon before drawing,
    // and skips the marker entirely when worldX is the -1 sentinel.
    private const int LoadedMarkerIconBias = 2;
    private const short NoMarkerSentinel = -1;

    public override SaveGame Extract(string id, Stream resourceStream) {
        long streamLength = resourceStream.Length;
        bool hasHeader = streamLength > TempGameDataSize;

        string saveGameName;
        short chapterNumber, worldX, worldY, mapIcon, version;

        if (hasHeader) {
            SaveGameHeader header = ReadHeader(resourceStream);
            saveGameName = header.Name;
            chapterNumber = header.ChapterNumber;
            worldX = header.WorldX;
            worldY = header.WorldY;
            mapIcon = header.MapIcon;
            version = header.Version;
        } else {
            saveGameName = string.Empty;
            chapterNumber = 0;
            worldX = 0;
            worldY = 0;
            mapIcon = 0;
            version = SaveGame.SupportedVersion;
        }

        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        long remaining = resourceStream.Length - resourceStream.Position;
        byte[] tempGameData = resourceReader.ReadBytes((int)remaining);
        SaveGameData? data = ParseData(tempGameData);

        // Only files with a header carry a world-map marker; a bare TEMP.GAM has none.
        FullMapIcon? fullMapMarker = hasHeader ? BuildFullMapMarker(worldX, worldY, mapIcon) : null;

        return new SaveGame(id, saveGameName, chapterNumber, worldX, worldY, mapIcon, version, tempGameData, data, fullMapMarker);
    }

    /// <summary>
    /// Reads just the 100-byte slot header from the start of <paramref name="resourceStream"/>,
    /// leaving the stream positioned immediately after it. Cheap alternative to
    /// <see cref="Extract"/> for save-slot listing — does not parse the ~300 KB body. Does not
    /// validate the version; consult <see cref="SaveGameHeader.IsSupportedVersion"/>.
    /// </summary>
    public static SaveGameHeader ReadHeader(Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage), leaveOpen: true);
        // DOS save names are null-terminated C-strings in a fixed 90-byte field. Truncate at the
        // first NUL (via the shared reader) rather than only trimming trailing NULs — otherwise a
        // leading/embedded NUL (e.g. a corrupt save with a zeroed char) leaks into the display name.
        string name = ReadFixedLengthString(reader.ReadBytes(SaveGameHeader.NameLength));
        short chapterNumber = reader.ReadInt16();
        short worldX = reader.ReadInt16();
        short worldY = reader.ReadInt16();
        short mapIcon = reader.ReadInt16();
        short version = reader.ReadInt16();
        return new SaveGameHeader(name, chapterNumber, worldX, worldY, mapIcon, version);
    }

    /// <summary>
    /// Builds the world-map marker from a save header. The header stores the party pin as
    /// pixel coordinates on the 320x200 FULLMAP.SCX plus an icon index; the original game
    /// adds 2 to that icon on load and hides the marker when worldX is -1
    /// (StartGameOrLoadSave 0x20835). Output is percentages so consumers hold no VGA knowledge.
    /// </summary>
    public static FullMapIcon BuildFullMapMarker(short worldX, short worldY, short mapIcon) {
        if (worldX == NoMarkerSentinel) {
            return new FullMapIcon(visible: false, xPercent: 0f, yPercent: 0f, iconIndex: 0);
        }

        return new FullMapIcon(
            visible: true,
            xPercent: worldX / FullMapWidth * 100f,
            yPercent: worldY / FullMapHeight * 100f,
            iconIndex: mapIcon + LoadedMarkerIconBias
        );
    }

    private static SaveGameData? ParseData(byte[] tempGameData) {
        if (tempGameData.Length < TempGameDataSize) {
            return null;
        }

        byte[] stateDataBytes = new byte[StateDataSize];
        byte[] worldDataBytes = new byte[WorldDataSize];
        byte[] actorDataBytes = new byte[ActorDataSize];
        byte[] combatDataBytes = new byte[CombatDataSize];
        byte[] zoneContainerDataBytes = new byte[ZoneContainerDataSize];

        int offset = 0;
        Buffer.BlockCopy(tempGameData, offset, stateDataBytes, 0, StateDataSize);
        offset += StateDataSize;
        Buffer.BlockCopy(tempGameData, offset, worldDataBytes, 0, WorldDataSize);
        offset += WorldDataSize;
        Buffer.BlockCopy(tempGameData, offset, actorDataBytes, 0, ActorDataSize);
        offset += ActorDataSize;
        Buffer.BlockCopy(tempGameData, offset, combatDataBytes, 0, CombatDataSize);
        offset += CombatDataSize;
        Buffer.BlockCopy(tempGameData, offset, zoneContainerDataBytes, 0, ZoneContainerDataSize);

        SaveGameStateData stateData = ParseStateData(stateDataBytes);
        SaveGameWorldData worldStateData = ParseWorldData(worldDataBytes);
        SaveGameActorData[] actorStateData = ParseActorData(actorDataBytes);
        SaveGameCombatData[] combatStateData = ParseCombatData(combatDataBytes);
        SaveGameZoneContainerStateData zoneContainerStateData = ParseZoneContainerStateData(zoneContainerDataBytes, worldStateData);

        return new SaveGameData(stateData, worldStateData, actorStateData, combatStateData, zoneContainerStateData, worldDataBytes, actorDataBytes, combatDataBytes, zoneContainerDataBytes);
    }

    private static SaveGameWorldData ParseWorldData(byte[] worldDataBytes) {
        using var worldStream = new MemoryStream(worldDataBytes);
        using var worldReader = new BinaryReader(worldStream, Encoding.GetEncoding(DosCodePage));

        byte[] zoneNumbers = worldReader.ReadBytes(ExplorationStateEntryCount);
        byte[] xCoordinates = worldReader.ReadBytes(ExplorationStateEntryCount);
        byte[] yCoordinates = worldReader.ReadBytes(ExplorationStateEntryCount);
        var exploredTileBitfields = new byte[ExplorationStateEntryCount][];
        for (var i = 0; i < ExplorationStateEntryCount; i++) {
            exploredTileBitfields[i] = worldReader.ReadBytes(ExploredTileBitfieldSize);
        }

        var explorationStateEntries = new List<SaveGameExplorationStateEntryData>(ExplorationStateEntryCount);
        for (var i = 0; i < ExplorationStateEntryCount; i++) {
            if (zoneNumbers[i] == byte.MaxValue) {
                continue;
            }

            explorationStateEntries.Add(new SaveGameExplorationStateEntryData(
                zoneNumbers[i],
                xCoordinates[i],
                yCoordinates[i],
                exploredTileBitfields[i]
            ));
        }

        var zoneDataEntries = new SaveGameZoneDataData[ZoneDataEntryCount];
        for (var i = 0; i < ZoneDataEntryCount; i++) {
            zoneDataEntries[i] = ParseZoneData(worldReader);
        }

        var chapterGoldAmounts = new int[ChapterGoldEntryCount];
        for (var i = 0; i < ChapterGoldEntryCount; i++) {
            chapterGoldAmounts[i] = worldReader.ReadInt32();
        }

        var enemyPartyActorSlots = new Dictionary<int, List<short>>(EnemyPartyRecordCount);
        for (var recordIndex = 0; recordIndex < EnemyPartyRecordCount; recordIndex++) {
            var actorSlots = new List<short>(EnemyPartySlotsPerRecord);
            for (var slotIndex = 0; slotIndex < EnemyPartySlotsPerRecord; slotIndex++) {
                short actorSlot = worldReader.ReadInt16();
                if (actorSlot != -1) {
                    actorSlots.Add(actorSlot);
                }
            }

            enemyPartyActorSlots[recordIndex] = actorSlots;
        }

        var combatEncounterTimestamps = new Dictionary<int, int>(EnemyPartyRecordCount);
        for (var encounterIndex = 0; encounterIndex < EnemyPartyRecordCount; encounterIndex++) {
            int timestamp = worldReader.ReadInt32();
            if (timestamp != 0) {
                combatEncounterTimestamps[encounterIndex] = timestamp;
            }
        }

        var enemyTrapRespawnTimestamps = new Dictionary<int, int>(EnemyPartyRecordCount);
        for (var encounterIndex = 0; encounterIndex < EnemyPartyRecordCount; encounterIndex++) {
            int timestamp = worldReader.ReadInt32();
            if (timestamp != 0) {
                enemyTrapRespawnTimestamps[encounterIndex] = timestamp;
            }
        }

        var worldEncounterItemData = new SaveGameWorldEncounterItemData[WorldEncounterZoneCount][];
        for (var zoneIndex = 0; zoneIndex < WorldEncounterZoneCount; zoneIndex++) {
            worldEncounterItemData[zoneIndex] = new SaveGameWorldEncounterItemData[WorldEncounterItemsPerZone];
            for (var itemIndex = 0; itemIndex < WorldEncounterItemsPerZone; itemIndex++) {
                worldEncounterItemData[zoneIndex][itemIndex] = new SaveGameWorldEncounterItemData(
                    worldReader.ReadInt32(),
                    worldReader.ReadInt32(),
                    worldReader.ReadInt16(),
                    worldReader.ReadInt16()
                );
            }
        }

        return new SaveGameWorldData(
            explorationStateEntries.ToArray(),
            zoneDataEntries,
            chapterGoldAmounts,
            enemyPartyActorSlots,
            combatEncounterTimestamps,
            enemyTrapRespawnTimestamps,
            worldEncounterItemData
        );
    }

    private static SaveGameStateData ParseStateData(byte[] stateDataBytes) {
        using var sectionStream = new MemoryStream(stateDataBytes);
        using var sectionReader = new BinaryReader(sectionStream, Encoding.GetEncoding(DosCodePage));

        short chapterNumber = sectionReader.ReadInt16();
        int partyGold = sectionReader.ReadInt32();
        int gameTimeIn2Seconds = sectionReader.ReadInt32();
        int timeSnapshot = sectionReader.ReadInt32();
        byte partyDeathState = sectionReader.ReadByte();
        byte chapterTransitionPending = sectionReader.ReadByte();
        byte padding = sectionReader.ReadByte();
        byte maxZoneNumber = sectionReader.ReadByte();
        byte currentZoneNumber = sectionReader.ReadByte();
        byte worldXCoordinate = sectionReader.ReadByte();
        byte worldYCoordinate = sectionReader.ReadByte();
        int positionX = sectionReader.ReadInt32();
        int positionY = sectionReader.ReadInt32();
        int positionZ = sectionReader.ReadInt32();
        short currentZRotation = sectionReader.ReadInt16();

        int zoneNumber = sectionReader.ReadByte();
        int destinationX = sectionReader.ReadByte();
        int destinationY = sectionReader.ReadByte();
        int destinationXOffset = sectionReader.ReadByte();
        int destinationYOffset = sectionReader.ReadByte();
        int destinationZRotation = sectionReader.ReadInt16();
        int gdsNumber = sectionReader.ReadInt16();
        int gdsLetter = sectionReader.ReadInt16();

        var teleportDestination = new TeleportDestination {
            Location = new Location {
                ZoneNumber = zoneNumber,
                X = destinationX,
                Y = destinationY,
                XOffset = destinationXOffset,
                YOffset = destinationYOffset,
                ZRotation = destinationZRotation
            },
            GdsNumber = gdsNumber,
            GdsLetter = gdsLetter,
            Id = 0
        };

        short stepSize = sectionReader.ReadInt16();
        short turnSize = sectionReader.ReadInt16();

        var movementData = new SaveGameMovementData(
            sectionReader.ReadInt16(),
            sectionReader.ReadByte(),
            sectionReader.ReadInt16(),
            sectionReader.ReadInt32()
        );

        var actorNames = new string[PartyActorCount];
        for (var actorIndex = 0; actorIndex < actorNames.Length; actorIndex++) {
            actorNames[actorIndex] = ReadFixedLengthString(sectionReader.ReadBytes(ActorNameLength));
        }

        byte[] partyActorData = sectionReader.ReadBytes(PartyActorDataSize);
        SaveGameActorData[] partyActors = ParsePartyActors(partyActorData);

        byte numberOfActivePartyCharacters = sectionReader.ReadByte();
        byte[] activePartyCharacters = sectionReader.ReadBytes(3);
        uint sharedInventoryPointer1 = sectionReader.ReadUInt32();
        uint sharedInventoryPointer2 = sectionReader.ReadUInt32();
        byte attributeIncreasedFlag = sectionReader.ReadByte();
        short rewardMoneyCounter = sectionReader.ReadInt16();
        short[] initialAttributeGainModifiers = {
            sectionReader.ReadInt16(),
            sectionReader.ReadInt16()
        };
        byte unusedPadding = sectionReader.ReadByte();
        var actor0StatusEffects = new SaveGameActorStatusEffectsData(
            sectionReader.ReadByte(),
            sectionReader.ReadByte(),
            sectionReader.ReadByte(),
            sectionReader.ReadByte(),
            sectionReader.ReadByte(),
            sectionReader.ReadByte(),
            sectionReader.ReadByte()
        );

        var actorStatusEffects = new SaveGameActorStatusEffectsData[PartyActorCount];
        actorStatusEffects[0] = actor0StatusEffects;
        for (var actorIndex = 1; actorIndex < PartyActorCount; actorIndex++) {
            actorStatusEffects[actorIndex] = new SaveGameActorStatusEffectsData(
                sectionReader.ReadByte(),
                sectionReader.ReadByte(),
                sectionReader.ReadByte(),
                sectionReader.ReadByte(),
                sectionReader.ReadByte(),
                sectionReader.ReadByte(),
                sectionReader.ReadByte()
            );
        }

        // Remaining 7 bytes in this 42-byte block are documented as trailing/unused.
        sectionReader.ReadBytes(ActorStatusEffectsSize);

        var partyConfigurationData = new SaveGamePartyConfigurationData(
            numberOfActivePartyCharacters,
            activePartyCharacters,
            sharedInventoryPointer1,
            sharedInventoryPointer2,
            attributeIncreasedFlag,
            rewardMoneyCounter,
            initialAttributeGainModifiers,
            unusedPadding,
            actorStatusEffects
        );

        SaveGameTimedEffectData[][] timedEffectsByActor = ParseTimedEffects(sectionReader);
        SaveGameMiscStateData miscStateData = ParseMiscStateData(sectionReader);
        short currentTimerAmount = sectionReader.ReadInt16();
        SaveGameTimerData[] timers = ParseTimers(sectionReader);
        SaveGameLightingStateData lightingStateData = ParseLightingStateData(sectionReader);
        SaveGameZoneDataData cachedZoneData = ParseZoneData(sectionReader);
        byte[] globalFlags = sectionReader.ReadBytes(GlobalFlagsSize);
        byte[] globalFlags2 = sectionReader.ReadBytes(GlobalFlags2Size);

        return new SaveGameStateData(
            chapterNumber,
            partyGold,
            gameTimeIn2Seconds,
            timeSnapshot,
            partyDeathState,
            chapterTransitionPending,
            padding,
            maxZoneNumber,
            currentZoneNumber,
            worldXCoordinate,
            worldYCoordinate,
            positionX,
            positionY,
            positionZ,
            currentZRotation,
            teleportDestination,
            stepSize,
            turnSize,
            movementData,
            actorNames,
            partyActorData,
            partyActors,
            partyConfigurationData,
            timedEffectsByActor,
            miscStateData,
            currentTimerAmount,
            timers,
            lightingStateData,
            cachedZoneData,
            globalFlags,
            globalFlags2
        );
    }

    private static SaveGameTimedEffectData[][] ParseTimedEffects(BinaryReader reader) {
        var timedEffectsByActor = new SaveGameTimedEffectData[PartyActorCount][];

        for (var actorIndex = 0; actorIndex < PartyActorCount; actorIndex++) {
            timedEffectsByActor[actorIndex] = new SaveGameTimedEffectData[TimedEffectsPerActor];
            for (var effectIndex = 0; effectIndex < TimedEffectsPerActor; effectIndex++) {
                timedEffectsByActor[actorIndex][effectIndex] = ParseTimedEffect(reader);
            }
        }

        return timedEffectsByActor;
    }

    private static SaveGameTimedEffectData ParseTimedEffect(BinaryReader reader) {
        return new SaveGameTimedEffectData(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    private static SaveGameMiscStateData ParseMiscStateData(BinaryReader reader) {
        return new SaveGameMiscStateData(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    private static SaveGameTimerData[] ParseTimers(BinaryReader reader) {
        var timers = new SaveGameTimerData[TimerCount];
        for (var timerIndex = 0; timerIndex < TimerCount; timerIndex++) {
            timers[timerIndex] = ParseTimer(reader);
        }

        return timers;
    }

    private static SaveGameTimerData ParseTimer(BinaryReader reader) {
        var type = (TimerType)reader.ReadByte();
        var flag = (TimerFlag)reader.ReadByte();
        short key = reader.ReadInt16();
        int time = reader.ReadInt32();
        return new SaveGameTimerData(type, flag, key, time);
    }

    private static SaveGameLightingStateData ParseLightingStateData(BinaryReader reader) {
        return new SaveGameLightingStateData(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16()
        );
    }

    private static SaveGameZoneDataData ParseZoneData(BinaryReader reader) {
        return new SaveGameZoneDataData(
            reader.ReadInt16(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    private static SaveGameActorData[] ParseActorData(byte[] actorDataBytes) {
        var actors = new SaveGameActorData[ActorSlotCount];
        using var actorStream = new MemoryStream(actorDataBytes);
        using var actorReader = new BinaryReader(actorStream, Encoding.GetEncoding(DosCodePage));

        for (var i = 0; i < actors.Length; i++) {
            actors[i] = ActorRecordReader.ParseActor(actorReader);
        }

        return actors;
    }

    private static SaveGameActorData[] ParsePartyActors(byte[] partyActorData) {
        var actors = new SaveGameActorData[PartyActorCount];
        using var actorStream = new MemoryStream(partyActorData);
        using var actorReader = new BinaryReader(actorStream, Encoding.GetEncoding(DosCodePage));

        for (var i = 0; i < actors.Length; i++) {
            actors[i] = ActorRecordReader.ParseActor(actorReader);
        }

        return actors;
    }


    private static SaveGameCombatData[] ParseCombatData(byte[] combatDataBytes) {
        var combatData = new SaveGameCombatData[CombatSlotCount];
        using var combatStream = new MemoryStream(combatDataBytes);
        using var combatReader = new BinaryReader(combatStream, Encoding.GetEncoding(DosCodePage));

        for (var i = 0; i < combatData.Length; i++) {
            combatData[i] = ParseCombat(combatReader);
        }

        return combatData;
    }

    private static SaveGameCombatData ParseCombat(BinaryReader reader) {
        return new SaveGameCombatData(
            reader.ReadUInt16(),
            reader.ReadInt16(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadInt16(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSByte()
        );
    }

    private static SaveGameZoneContainerStateData ParseZoneContainerStateData(byte[] zoneContainerDataBytes, SaveGameWorldData worldStateData) {
        using var zoneContainerStream = new MemoryStream(zoneContainerDataBytes);
        using var zoneContainerReader = new BinaryReader(zoneContainerStream, Encoding.GetEncoding(DosCodePage));

        var zones = new List<SaveGameZoneContainerEntryData>(worldStateData.ZoneDataEntries.Length);
        foreach (SaveGameZoneDataData zoneData in worldStateData.ZoneDataEntries) {
            int zoneOffset = NormalizeZoneContainerOffset(zoneData.TempGamFileOffset, zoneContainerDataBytes.Length);
            if (zoneOffset < 0 || zoneOffset >= zoneContainerDataBytes.Length - 2) {
                continue;
            }

            zoneContainerReader.BaseStream.Position = zoneOffset;
            short numberOfContainers = zoneContainerReader.ReadInt16();
            if (numberOfContainers <= 0) {
                continue;
            }

            var containers = new SaveGameContainerData[numberOfContainers];
            for (var containerIndex = 0; containerIndex < numberOfContainers; containerIndex++) {
                containers[containerIndex] = ParseContainer(zoneContainerReader);
            }

            zones.Add(new SaveGameZoneContainerEntryData(zoneData.ZoneNumber, zoneData.TempGamFileOffset, containers));
        }

        return new SaveGameZoneContainerStateData(zones.ToArray());
    }

    private static int NormalizeZoneContainerOffset(int tempGamOffset, int zoneContainerDataLength) {
        if (tempGamOffset >= 0 && tempGamOffset < zoneContainerDataLength) {
            return tempGamOffset;
        }

        int sectionLocalOffset = tempGamOffset - ZoneContainerSectionStartOffset;
        if (sectionLocalOffset >= 0 && sectionLocalOffset < zoneContainerDataLength) {
            return sectionLocalOffset;
        }

        return -1;
    }

    private static SaveGameContainerData ParseContainer(BinaryReader reader) {
        byte locationZone = reader.ReadByte();
        byte locationMinMaxChapter = reader.ReadByte();
        short locationWorldItemId = reader.ReadInt16();
        int locationX = reader.ReadInt32();
        int locationY = reader.ReadInt32();

        var location = new SaveGameContainerLocationData(
            locationZone,
            locationMinMaxChapter >> 4,
            locationMinMaxChapter & 0x0F,
            locationWorldItemId,
            locationX,
            locationY,
            (short)(locationX & 0xFFFF)
        );

        var containerType = (SaveGameContainerType)reader.ReadByte();
        byte numberOfItems = reader.ReadByte();
        byte capacity = reader.ReadByte();
        var dataTypes = (SaveGameContainerDataType)reader.ReadByte();

        var items = new SaveGameInventoryItemData[capacity];
        for (var itemIndex = 0; itemIndex < capacity; itemIndex++) {
            items[itemIndex] = new SaveGameInventoryItemData(
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadUInt16()
            );
        }

        SaveGameContainerLockData? lockData = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Lock)) {
            lockData = new SaveGameContainerLockData(
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            );
        }

        SaveGameContainerDialogData? dialogData = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Dialog)) {
            dialogData = new SaveGameContainerDialogData(
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadUInt32()
            );
        }

        SaveGameContainerShopData? shopData = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Shop)) {
            shopData = new SaveGameContainerShopData(
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                (GameData.ShopItemCategories)reader.ReadUInt16()
            );
        }

        SaveGameContainerEncounterData? encounterData = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Encounter)) {
            encounterData = new SaveGameContainerEncounterData(
                reader.ReadInt16(),
                reader.ReadInt16(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            );
        }

        int? timestamp = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Timestamp)) {
            timestamp = reader.ReadInt32();
        }

        short? unknown20 = null;
        if (dataTypes.HasFlag(SaveGameContainerDataType.Unknown20)) {
            unknown20 = reader.ReadInt16();
        }

        return new SaveGameContainerData(
            location,
            containerType,
            numberOfItems,
            capacity,
            dataTypes,
            items,
            lockData,
            dialogData,
            shopData,
            encounterData,
            timestamp,
            unknown20
        );
    }

    private static string ReadFixedLengthString(byte[] data) {
        string value = Encoding.GetEncoding(DosCodePage).GetString(data);
        int zeroIndex = value.IndexOf('\0');
        if (zeroIndex >= 0) {
            value = value[..zeroIndex];
        }

        return value.TrimEnd();
    }
}
