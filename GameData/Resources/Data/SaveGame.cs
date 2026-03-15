namespace GameData.Resources.Data;

using GameData.Resources;

using System;
using System.Text.Json.Serialization;

public class SaveGame : IResource {
    public const short SupportedVersion = 0x16;

    public SaveGame(
        string id,
        string saveGameName,
        short chapterNumber,
        short worldX,
        short worldY,
        short mapIcon,
        short version,
        byte[] tempGameData,
        SaveGameData? data
    ) {
        Id = id;
        SaveGameName = saveGameName;
        ChapterNumber = chapterNumber;
        WorldX = worldX;
        WorldY = worldY;
        MapIcon = mapIcon;
        Version = version;
        TempGameData = tempGameData ?? Array.Empty<byte>();
        Data = data;
    }

    public string SaveGameName { get; }
    public short ChapterNumber { get; }
    public short WorldX { get; }
    public short WorldY { get; }
    public short MapIcon { get; }
    public short Version { get; }
    public bool IsSupportedVersion { get => Version == SupportedVersion; }

    [JsonIgnore]
    public byte[] TempGameData { get; }

    public int TempGameDataLength { get => TempGameData.Length; }

    public SaveGameData? Data { get; }

    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }
}
