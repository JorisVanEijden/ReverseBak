namespace GameData.Resources.Data;

public class SaveGameAttributeValuesData {
    public SaveGameAttributeValuesData(
        byte maximum,
        byte current,
        byte currentEffective,
        byte experience,
        byte modifier
    ) {
        Maximum = maximum;
        Current = current;
        CurrentEffective = currentEffective;
        Experience = experience;
        Modifier = modifier;
    }

    public byte Maximum { get; }
    public byte Current { get; }
    public byte CurrentEffective { get; }
    public byte Experience { get; }
    public byte Modifier { get; }
}
