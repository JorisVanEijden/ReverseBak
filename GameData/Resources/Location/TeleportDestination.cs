namespace GameData.Resources.Location;

public class TeleportDestination {
    public Location Location { get; set; } = null!; // always set by TeleportExtractor's initializer / the JSON reader
    public int GdsNumber { get; set; }
    public int GdsLetter { get; set; }
    public int Id { get; set; }
}