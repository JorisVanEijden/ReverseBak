namespace GameData.Resources.World;

public class ZoneMap : IResource
{
    public const int Rows = 50;
    public const int BytesPerRow = 8;
    public const int TotalBytes = Rows * BytesPerRow;

    public ZoneMap(string id) { Id = id; BitmapData = new byte[TotalBytes]; }
    public byte[] BitmapData { get; set; }
    public bool IsTileInZone(int x, int y)
    {
        int byteIndex = y * BytesPerRow + x / 8;
        int bit = x % 8;
        return (BitmapData[byteIndex] >> bit & 1) == 1;
    }
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
