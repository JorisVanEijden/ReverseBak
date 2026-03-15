namespace GameData.Resources.Data;

public class SaveGameContainerLocationData {
    public SaveGameContainerLocationData(
        int zone,
        int minChapter,
        int maxChapter,
        short worldItemId,
        int x,
        int y,
        short actorNumber
    ) {
        Zone = zone;
        MinChapter = minChapter;
        MaxChapter = maxChapter;
        WorldItemId = worldItemId;
        X = x;
        Y = y;
        ActorNumber = actorNumber;
    }

    public int Zone { get; }
    public int MinChapter { get; }
    public int MaxChapter { get; }
    public short WorldItemId { get; }
    public int X { get; }
    public int Y { get; }
    public short ActorNumber { get; }
}
