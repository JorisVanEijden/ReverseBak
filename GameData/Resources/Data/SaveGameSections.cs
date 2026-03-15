namespace GameData.Resources.Data;

using System.Text.Json.Serialization;

public class SaveGameData {
    public SaveGameData(
        SaveGameStateData stateData,
        SaveGameWorldData worldStateData,
        SaveGameActorData[] actorStateData,
        SaveGameCombatData[] combatStateData,
        SaveGameZoneContainerStateData zoneContainerStateData,
        byte[] worldData,
        byte[] actorData,
        byte[] combatData,
        byte[] zoneContainerData
    ) {
        StateData = stateData;
        WorldStateData = worldStateData;
        ActorStateData = actorStateData;
        CombatStateData = combatStateData;
        ZoneContainerStateData = zoneContainerStateData;
        WorldData = worldData;
        ActorData = actorData;
        CombatData = combatData;
        ZoneContainerData = zoneContainerData;
    }

    public SaveGameStateData StateData { get; }
    public SaveGameWorldData WorldStateData { get; }
    public SaveGameActorData[] ActorStateData { get; }
    public SaveGameCombatData[] CombatStateData { get; }
    public SaveGameZoneContainerStateData ZoneContainerStateData { get; }

    [JsonIgnore]
    public byte[] WorldData { get; }

    [JsonIgnore]
    public byte[] ActorData { get; }

    [JsonIgnore]
    public byte[] CombatData { get; }

    [JsonIgnore]
    public byte[] ZoneContainerData { get; }

    public int WorldDataLength { get => WorldData.Length; }
    public int ActorDataLength { get => ActorData.Length; }
    public int CombatDataLength { get => CombatData.Length; }
    public int ZoneContainerDataLength { get => ZoneContainerData.Length; }
}
