namespace GameData.Resources.World;

public class WorldItem
{
    public ushort TypeId { get; set; }
    public Rotation3D Rotation { get; set; } = new();
    public Position3D Position { get; set; } = new();
}
