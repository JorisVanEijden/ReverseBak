namespace GameData.Resources.World;

public class ZoneMap : IResource
{
    public const int Width = 64;
    public const int Height = 50;

    public ZoneMap(string id)
    {
        Id = id;
        Tiles = new bool[Width][];
        for (int i = 0; i < Width; i++)
            Tiles[i] = new bool[Height];
    }
    public bool[][] Tiles { get; set; }
    public bool IsTileInZone(int x, int y) => Tiles[x][y];
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
