namespace ResourceExtraction.Extractors;

using GameData.Resources.Audio;
using System.IO;

public class ChapterSongMapExtractor : ExtractorBase<ChapterSongMap> {
    public const int ChapterCount = 9;

    public override ChapterSongMap Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var map = new ChapterSongMap(id);
        for (int i = 0; i < ChapterCount; i++) {
            map.Entries.Add(new ChapterSongEntry {
                Book1Song = reader.ReadInt16(),
                Book2Song = reader.ReadInt16(),
            });
        }
        return map;
    }
}
