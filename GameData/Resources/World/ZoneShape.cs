namespace GameData.Resources.World;

public class ZoneShape : IResource
{
    public ZoneShape(string id) { Id = id; }
    public List<ChapterMonsters> Chapters { get; set; } = new();
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
