namespace GameData.Resources.World;

// One chapter's worth of triggers from a Tzzxxyy.DAT file. The DOS loader
// only reads (2 + Triggers.Count * 19) bytes per chapter — the remaining
// bytes of each 192-byte chapter slot are unloaded leftover and are
// intentionally not represented here.
public class TileEventChapter
{
    public int ChapterNumber { get; set; }
    public List<TileEventTrigger> Triggers { get; set; } = new();
}
