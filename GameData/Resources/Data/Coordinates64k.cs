namespace GameData.Resources.Data;

// 8-byte coordinate pair used in DEF_TRAP entries.
// Mirrors IDA's `coordinates_64k` struct.
public class Coordinates64k {
    public int X { get; set; }
    public int Y { get; set; }
}
