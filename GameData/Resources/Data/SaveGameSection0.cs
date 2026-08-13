namespace GameData.Resources.Data;

using GameData.Resources.Location;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class SaveGameStateData {
    public SaveGameStateData(
        short chapterNumber,
        int partyGold,
        int gameTimeIn2Seconds,
        int timeSnapshot,
        byte partyDeathState,
        byte chapterTransitionPending,
        byte padding,
        byte maxZoneNumber,
        byte currentZoneNumber,
        byte worldXCoordinate,
        byte worldYCoordinate,
        int positionX,
        int positionY,
        int positionZ,
        short currentZRotation,
        TeleportDestination teleportDestination,
        short stepSize,
        short turnSize,
        SaveGameMovementData movementData,
        string[] actorNames,
        byte[] partyActorData,
        SaveGameActorData[] partyActors,
        SaveGamePartyConfigurationData partyConfigurationData,
        SaveGameTimedEffectData[][] timedEffectsByActor,
        SaveGameMiscStateData miscStateData,
        short currentTimerAmount,
        SaveGameTimerData[] timers,
        SaveGameLightingStateData lightingStateData,
        SaveGameZoneDataData cachedZoneData,
        byte[] globalFlags,
        byte[] globalFlags2
    ) {
        ChapterNumber = chapterNumber;
        PartyGold = partyGold;
        GameTimeIn2Seconds = gameTimeIn2Seconds;
        TimeSnapshot = timeSnapshot;
        PartyDeathState = partyDeathState;
        ChapterTransitionPending = chapterTransitionPending;
        Padding = padding;
        MaxZoneNumber = maxZoneNumber;
        CurrentZoneNumber = currentZoneNumber;
        WorldXCoordinate = worldXCoordinate;
        WorldYCoordinate = worldYCoordinate;
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        CurrentZRotation = currentZRotation;
        TeleportDestination = teleportDestination;
        StepSize = stepSize;
        TurnSize = turnSize;
        MovementData = movementData;
        ActorNames = actorNames ?? Array.Empty<string>();
        PartyActorData = partyActorData ?? Array.Empty<byte>();
        PartyActors = partyActors ?? Array.Empty<SaveGameActorData>();
        PartyConfigurationData = partyConfigurationData;
        TimedEffectsByActor = timedEffectsByActor ?? Array.Empty<SaveGameTimedEffectData[]>();
        MiscStateData = miscStateData;
        CurrentTimerAmount = currentTimerAmount;
        Timers = timers ?? Array.Empty<SaveGameTimerData>();
        LightingStateData = lightingStateData;
        CachedZoneData = cachedZoneData;
        GlobalFlags = globalFlags ?? Array.Empty<byte>();
        GlobalFlags2 = globalFlags2 ?? Array.Empty<byte>();
    }

    public short ChapterNumber { get; }
    public int PartyGold { get; }
    public int GameTimeIn2Seconds { get; }
    public int TimeSnapshot { get; }
    public byte PartyDeathState { get; }
    public byte ChapterTransitionPending { get; }
    public byte Padding { get; }
    public byte MaxZoneNumber { get; }
    public byte CurrentZoneNumber { get; }
    public byte WorldXCoordinate { get; }
    public byte WorldYCoordinate { get; }
    public int PositionX { get; }
    public int PositionY { get; }
    public int PositionZ { get; }
    public short CurrentZRotation { get; }
    public TeleportDestination TeleportDestination { get; }
    public short StepSize { get; }
    public short TurnSize { get; }
    public SaveGameMovementData MovementData { get; }
    public string[] ActorNames { get; }

    [JsonIgnore]
    public byte[] PartyActorData { get; }

    public SaveGameActorData[] PartyActors { get; }
    public SaveGamePartyConfigurationData PartyConfigurationData { get; }
    public SaveGameTimedEffectData[][] TimedEffectsByActor { get; }
    public SaveGameMiscStateData MiscStateData { get; }
    public short CurrentTimerAmount { get; }
    public SaveGameTimerData[] Timers { get; }
    public SaveGameLightingStateData LightingStateData { get; }
    public SaveGameZoneDataData CachedZoneData { get; }

    [JsonIgnore]
    public byte[] GlobalFlags { get; }

    [JsonIgnore]
    public byte[] GlobalFlags2 { get; }

    public int PartyActorDataLength { get => PartyActorData.Length; }
    public int GlobalFlagsLength { get => GlobalFlags.Length; }
    public int GlobalFlags2Length { get => GlobalFlags2.Length; }

    public Dictionary<int, int> DocumentedGlobalValues {
        get {
            var values = new Dictionary<int, int>();

            int[] keys = {
                30000, 30001, 30002, 30003, 30004, 30005, 30007, 30009, 30010,
                30012, 30013, 30014, 30015, 30018, 30019, 30029
            };

            foreach (int key in keys) {
                int? value = TryGetGlobalValue(key);
                if (value.HasValue) {
                    values[key] = value.Value;
                }
            }

            for (var key = 30020; key <= 30028; key++) {
                values[key] = TryGetGlobalValue(key) ?? 0;
            }

            return values;
        }
    }

    public int? TryGetGlobalValue(int key) {
        if (TryReadFlag(GlobalFlags, key, out int flagValue)) {
            return flagValue;
        }

        if (key >= 56000 && TryReadFlag(GlobalFlags2, key - 56000, out int extendedFlagValue)) {
            return extendedFlagValue;
        }

        return key switch {
            30000 => MiscStateData.Global30000,
            // The dialog's primary actor (nEvtArgActor0, TEMP.GAM +0x0596): text-variable kinds 10
            // and 30, and the actor a bare '@' names.
            30004 => MiscStateData.DialogPrimaryActorNumber,
            30001 => Math.Min(ushort.MaxValue, Math.Max(0, PartyGold / 10)),
            30002 => Math.Min(ushort.MaxValue, Math.Max(0, PartyGold)),
            30003 => PartyGold >= MiscStateData.Global30014Money ? 1 : 0,
            // The chapter's designated speaker (GetGlobalValue @0x42399): member 3 whenever he is in
            // the active party, otherwise the chapter's own entry in the 9-byte table at 0x3a53e.
            // NOT the roster's first member — that only coincides in chapter 1.
            30005 => ChapterSpeaker(),
            30007 => ChapterNumber,
            30009 => IsNightTime() ? 1 : 0,
            30010 => IsNightTime() ? 0 : 1,
            30012 => GetCurrentHourOfDay(),
            30013 => MiscStateData.Global30013AttributeValue,
            30014 => MiscStateData.Global30014Money,
            30015 => MiscStateData.Global30015,
            30018 => MiscStateData.Global30018,
            30019 => CurrentZoneNumber,
            >= 30020 and <= 30028 => ChapterNumber == key - 30019 ? 1 : 0,
            30029 => PartyConfigurationData.RewardMoneyCounter > 0 ? 1 : 0,
            _ => null
        };
    }

    /// <summary>Member ids the chapters designate as their speaker, chapter 1 first
    /// (<c>chapterCharacterNumbers</c> @0x3a53e).</summary>
    private static readonly byte[] ChapterSpeakers = { 0, 4, 4, 1, 4, 2, 4, 2, 3 };

    /// <summary>Global 30005 — see <c>GetGlobalValue</c> @0x42399.</summary>
    private int ChapterSpeaker() {
        const int Pug = 3;
        foreach (byte member in PartyConfigurationData.ActivePartyCharacters) {
            if (member == Pug) {
                return Pug;
            }
        }
        int index = ChapterNumber - 1;
        return index >= 0 && index < ChapterSpeakers.Length ? ChapterSpeakers[index] : 0;
    }

    private int GetCurrentHourOfDay() => GameState.GameTime.HourOfDay(GameTimeIn2Seconds);

    private bool IsNightTime() {
        int hour = GetCurrentHourOfDay();
        return hour < 4 || hour >= 20;
    }

    private static bool TryReadFlag(byte[] bitfield, int bitIndex, out int value) {
        value = 0;
        if (bitIndex < 0) {
            return false;
        }

        int byteIndex = bitIndex / 8;
        if (byteIndex >= bitfield.Length) {
            return false;
        }

        int bitOffset = bitIndex % 8;
        value = (bitfield[byteIndex] >> bitOffset) & 1;
        return true;
    }
}
