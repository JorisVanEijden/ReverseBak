namespace GameData.Resources.Data;

public class SaveGameActorStatusEffectsData {
    public SaveGameActorStatusEffectsData(
        byte sick,
        byte plagued,
        byte poisoned,
        byte drunk,
        byte healing,
        byte starving,
        byte nearDeath
    ) {
        Sick = sick;
        Plagued = plagued;
        Poisoned = poisoned;
        Drunk = drunk;
        Healing = healing;
        Starving = starving;
        NearDeath = nearDeath;
    }

    public byte Sick { get; }
    public byte Plagued { get; }
    public byte Poisoned { get; }
    public byte Drunk { get; }
    public byte Healing { get; }
    public byte Starving { get; }
    public byte NearDeath { get; }
}
