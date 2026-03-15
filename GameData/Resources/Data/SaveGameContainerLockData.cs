namespace GameData.Resources.Data;

public class SaveGameContainerLockData {
    public SaveGameContainerLockData(
        byte flags,
        byte difficulty,
        byte puzzleChest,
        byte trapDamage
    ) {
        Flags = flags;
        Difficulty = difficulty;
        PuzzleChest = puzzleChest;
        TrapDamage = trapDamage;
    }

    public byte Flags { get; }
    public byte Difficulty { get; }
    public byte PuzzleChest { get; }
    public byte TrapDamage { get; }
}
