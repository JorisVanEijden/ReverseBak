namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;
using GameData.Resources.Location;

using System.IO;

/// <summary>
/// Parses CHAPx.DAT (23 bytes) — the per-chapter starting state read by the original
/// game's <c>go_to_chapter</c> (KRONDOR.EXE 0x41f0a). The engine reads the first 16 bytes
/// over its game-state globals (<c>readFromFile</c> count=0x10) and the trailing 7 bytes
/// as a <c>location</c> (count=1, size=7), then calls <c>SetPlayerLocation</c> (0x738b3).
///
/// Layout (little-endian):
/// <code>
/// +0x00  int16  chapterNumber
/// +0x02  int32  gold
/// +0x06  int32  gameTimeIn2Seconds      (chapter base clock)
/// +0x0A  int32  timeSnapshot
/// +0x0E  byte   partyDeathState
/// +0x0F  byte   chapterTransitionPending
/// +0x10  byte   zone
/// +0x11  byte   tileX
/// +0x12  byte   tileY
/// +0x13  byte   tileXOffset
/// +0x14  byte   tileYOffset
/// +0x15  int16  zRotation
/// </code>
/// The full-map marker is NOT in this file; see <see cref="GetFullMapIcon"/>.
/// </summary>
public class ChapterDataExtractor : ExtractorBase<ChapterStartData> {
    // Internal game-position units. ToGamePosition (0x72fee): each tile is 64000 units,
    // each sub-tile offset 1600; ToCenteredGamePosition (0x73035) then adds 800 to centre
    // the party within the sub-tile.
    private const int UnitsPerTile = 64000;
    private const int UnitsPerOffset = 1600;
    private const int SubTileCentreOffset = 800;

    // FULLMAP.SCX reference resolution; the per-chapter marker coordinates below are pixels
    // on it. Converted to percentages here so downstream consumers hold no VGA knowledge.
    private const float FullMapWidth = 320f;
    private const float FullMapHeight = 200f;

    public override ChapterStartData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);

        short chapterNumber = reader.ReadInt16();
        int gold = reader.ReadInt32();
        int gameTimeIn2Seconds = reader.ReadInt32();
        int timeSnapshot = reader.ReadInt32();
        byte partyDeathState = reader.ReadByte();
        byte chapterTransitionPending = reader.ReadByte();

        byte zone = reader.ReadByte();
        byte tileX = reader.ReadByte();
        byte tileY = reader.ReadByte();
        byte tileXOffset = reader.ReadByte();
        byte tileYOffset = reader.ReadByte();
        short zRotation = reader.ReadInt16();

        var startLocation = new Location {
            ZoneNumber = zone,
            X = tileX,
            Y = tileY,
            XOffset = tileXOffset,
            YOffset = tileYOffset,
            ZRotation = zRotation
        };

        int positionX = tileX * UnitsPerTile + tileXOffset * UnitsPerOffset + SubTileCentreOffset;
        int positionY = tileY * UnitsPerTile + tileYOffset * UnitsPerOffset + SubTileCentreOffset;

        FullMapIcon fullMapIcon = GetFullMapIcon(chapterNumber);

        return new ChapterStartData(
            id,
            chapterNumber,
            gold,
            gameTimeIn2Seconds,
            timeSnapshot,
            partyDeathState,
            chapterTransitionPending,
            startLocation,
            positionX,
            positionY,
            fullMapIcon
        );
    }

    /// <summary>
    /// The world-map marker for each chapter, hard-coded in <c>StartGameOrLoadSave</c>
    /// (0x20835): chapter 1 is the "new game" branch (116,75 icon 18); chapters 2-9 come
    /// from its per-chapter switch. Chapter 8 never sets a position — the map shows without
    /// a marker. Pixel coordinates are normalised to percentages of the 320x200 FULLMAP.SCX.
    /// </summary>
    private static FullMapIcon GetFullMapIcon(int chapter) {
        (int x, int y, int icon)? marker = chapter switch {
            1 => (116, 75, 18),
            2 => (168, 147, 18),
            3 => (256, 114, 2),
            4 => (167, 24, 26),
            5 => (234, 52, 2),
            6 => (168, 148, 2),
            7 => (184, 92, 18),
            9 => (203, 128, 2),
            _ => null // chapter 8 (and anything out of range): no marker
        };

        if (marker is null) {
            return new FullMapIcon(visible: false, xPercent: 0f, yPercent: 0f, iconIndex: 0);
        }

        (int x, int y, int icon) = marker.Value;
        return new FullMapIcon(
            visible: true,
            xPercent: x / FullMapWidth * 100f,
            yPercent: y / FullMapHeight * 100f,
            iconIndex: icon
        );
    }
}
