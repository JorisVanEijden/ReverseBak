namespace ResourceExtractor.Extractors.Container;

public class LockData {
    public int Difficulty { get; set; }
    public int PuzzleChest { get; set; }
    public int TrapDamage { get; set; }
    public LockFlag Flags { get; set; }
}

public enum LockFlag {
    Trapped = 4
}