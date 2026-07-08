namespace ResourceExtraction.Extractors;

using GameData.Resources.GameState;
using GameData.Resources.World;
using ResourceExtraction.Extractors.GameState;
using System.IO;
using System.Text;

// Parses Tzzxxyy.DAT — 1920 bytes = 10 chapter blocks of 192 bytes each.
// Per chapter block: u16 trigger count, then count * 19-byte trigger record.
// Bytes after (2 + count * 19) are unloaded leftovers in the original
// loader (ovr187:loadTzzxxyy.DAT @ 0x73950) and are deliberately ignored.
public class TileEventExtractor : ExtractorBase<TileEventTile>
{
    private const int ChapterCount = 10;
    private const int ChapterBlockSize = 192;
    private const int RecordSize = 19;
    private const int MaxRecordsPerChapter = (ChapterBlockSize - 2) / RecordSize; // 10

    public override TileEventTile Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var tile = new TileEventTile(id);

        string name = Path.GetFileNameWithoutExtension(id);
        if (name.Length >= 7 && (name[0] == 'T' || name[0] == 't'))
        {
            if (byte.TryParse(name.Substring(1, 2), out byte zone)) tile.ZoneNumber = zone;
            if (byte.TryParse(name.Substring(3, 2), out byte x)) tile.X = x;
            if (byte.TryParse(name.Substring(5, 2), out byte y)) tile.Y = y;
        }

        for (int chapterIndex = 0; chapterIndex < ChapterCount; chapterIndex++)
        {
            long blockStart = (long)chapterIndex * ChapterBlockSize;
            if (blockStart + 2 > resourceStream.Length) break;
            resourceStream.Position = blockStart;

            int count = reader.ReadUInt16();
            if (count > MaxRecordsPerChapter)
            {
                throw new InvalidDataException(
                    $"{id} chapter {chapterIndex + 1}: trigger count {count} exceeds {MaxRecordsPerChapter}");
            }

            var chapter = new TileEventChapter { ChapterNumber = chapterIndex + 1 };
            for (int i = 0; i < count; i++)
            {
                var type = (TileEventType)reader.ReadUInt16();
                byte startX = reader.ReadByte();
                byte endY = reader.ReadByte();
                byte endX = reader.ReadByte();
                byte startY = reader.ReadByte();
                uint entryNumber = reader.ReadUInt32();
                byte fireOnce = reader.ReadByte();
                ushort requiredKey = reader.ReadUInt16();
                ushort forbiddenKey = reader.ReadUInt16();
                ushort setOnFireKey = reader.ReadUInt16();
                ushort repeatable = reader.ReadUInt16();

                chapter.Triggers.Add(new TileEventTrigger
                {
                    Type = type,
                    StartX = startX,
                    EndY = endY,
                    EndX = endX,
                    StartY = startY,
                    EntryNumber = entryNumber,
                    FireOnce = fireOnce,
                    Requires = requiredKey == 0 ? null : GlobalRef.DecodeCondition(requiredKey, 1, null),
                    Forbids = forbiddenKey == 0 ? null : GlobalRef.DecodeCondition(forbiddenKey, 1, null),
                    OnFire = DefEffect.ForKey(setOnFireKey, set: true),
                    Repeatable = repeatable
                });
            }

            tile.Chapters.Add(chapter);
        }

        return tile;
    }
}
